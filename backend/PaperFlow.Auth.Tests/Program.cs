using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using PaperFlow.Api.Contracts;
using PaperFlow.Api.Security;
using PaperFlow.Api.Services;

// Uses a dedicated temporary database and a fake email transport; sends no real mail.
var databaseName = "PaperFlowAuthTests_" + Guid.NewGuid().ToString("N");
var mongoUri = Environment.GetEnvironmentVariable("PAPERFLOW_TEST_MONGO") ?? "mongodb://localhost:27017";
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["MongoDB:ConnectionString"] = mongoUri,
    ["MongoDB:DatabaseName"] = databaseName,
    ["Auth:OtpSecret"] = Crypto.NewToken(),
    ["Smtp:Username"] = "test@example.invalid",
    ["Smtp:Password"] = "fake-test-password"
}).Build();
var client = new MongoClient(mongoUri);
var store = new AuthStore(config);
var transport = new FakeMail(config);
var auth = new AuthService(store, config, transport);
void Check(bool condition, string label) { if (!condition) throw new Exception(label); }
const string password = "StrongTestPassword123!";
try
{
    var brevoConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
        ["Brevo:ApiKey"] = "fake-test-key", ["Brevo:SenderEmail"] = "sender@example.invalid"
    }).Build();
    var httpMail = new FakeBrevoHttp();
    var brevo = new BrevoEmailService(new HttpClient(httpMail), brevoConfig);
    await brevo.SendOtpAsync("recipient@example.invalid", "123456", default);
    Check(httpMail.Body.Contains("123456") && httpMail.Body.Contains("textContent"), "Brevo request carries OTP text");
    await brevo.SendPasswordResetAsync("recipient@example.invalid", "654321", default);
    Check(httpMail.Body.Contains("654321"), "Brevo reset delivery");
    foreach (var status in new[] { 400, 401, 429, 500 })
    {
        httpMail.Status = status;
        try { await brevo.SendOtpAsync("recipient@example.invalid", "123456", default); throw new Exception("Provider rejection must fail"); }
        catch (InvalidOperationException) { }
    }
    await store.EnsureIndexesAsync();
    foreach (var setting in new[] { "Auth:OtpSecret", "Smtp:Username", "Smtp:Password" })
    {
        var saved = config[setting];
        config[setting] = "";
        try
        {
            await auth.RegisterAsync(new RegisterRequest("missing-config@example.invalid", "Reader", password), default);
            throw new Exception("Missing configuration must reject registration");
        }
        catch (AuthConfigurationException) { }
        finally { config[setting] = saved; }
        Check(await store.Users.CountDocumentsAsync(_ => true) == 0, "Missing configuration creates no account");
        Check(await store.Otps.CountDocumentsAsync(_ => true) == 0, "Missing configuration creates no OTP");
        Check(transport.Sends == 0, "Missing configuration sends no email");
    }
    var timing = await auth.RegisterAsync(new RegisterRequest(" Reader@Example.invalid ", "Reader", password), default);
    Check(transport.Sends == 1, "Registration sends OTP");
    var issuedOtp = await store.Otps.Find(_ => true).FirstAsync();
    Check(Math.Abs((timing.ExpiresAt - issuedOtp.ExpiresAt).TotalMilliseconds) < 1, "Response uses persisted OTP expiry");
    Check(timing.ServerTime < timing.ExpiresAt, "Countdown starts before expiry");
    Check(await auth.LoginAsync("reader@example.invalid", password, default) is null, "Unverified account cannot login");
    var cooldownTiming = await auth.ResendOtpAsync("reader@example.invalid", default);
    Check(transport.Sends == 1, "Resend cooldown");
    Check(Math.Abs((cooldownTiming.ExpiresAt - timing.ExpiresAt).TotalMilliseconds) < 1, "Cooldown does not reset OTP expiry");
    var code = transport.Code;
    var wrongCode = code == "000000" ? "000001" : "000000";
    Check(!await auth.VerifyAsync("reader@example.invalid", wrongCode, default), "Wrong OTP rejected");
    Check(await auth.VerifyAsync("reader@example.invalid", code, default), "Valid OTP accepted");
    Check(!await auth.VerifyAsync("reader@example.invalid", code, default), "OTP replay rejected");
    Check(await auth.LoginAsync("reader@example.invalid", "wrong-password", default) is null, "Wrong password rejected");
    var login = await auth.LoginAsync("reader@example.invalid", password, default);
    Check(login is not null, "Verified account can login");
    var user = await auth.CurrentAsync(login!.Value.Token, default);
    Check(user is { Role: "User" }, "Session resolves user without admin privileges");
    var session = await store.Sessions.Find(_ => true).FirstAsync();
    Check(session.TokenHash != login.Value.Token, "Session token stored hashed");
    await auth.LogoutAsync(login.Value.Token, default);
    Check(await auth.CurrentAsync(login.Value.Token, default) is null, "Logout revokes session");

    await auth.RegisterAsync(new RegisterRequest("locked@example.invalid", "Locked", password), default);
    code = transport.Code;
    wrongCode = code == "000000" ? "000001" : "000000";
    for (var i = 0; i < 5; i++) Check(!await auth.VerifyAsync("locked@example.invalid", wrongCode, default), "Invalid attempt");
    Check(!await auth.VerifyAsync("locked@example.invalid", code, default), "OTP locked after five failures");
    var reset = new PasswordResetService(store, config, transport);
    var sendsBefore = transport.Sends;
    await reset.RequestAsync("unknown@example.invalid", default);
    await reset.RequestAsync("locked@example.invalid", default);
    Check(transport.Sends == sendsBefore, "Reset hides unknown and unverified accounts");
    var oldSession = await auth.LoginAsync("reader@example.invalid", password, default);
    await reset.RequestAsync("reader@example.invalid", default);
    code = transport.Code;
    await reset.RequestAsync("reader@example.invalid", default);
    Check(transport.Sends == sendsBefore + 1, "Reset resend cooldown");
    wrongCode = code == "000000" ? "000001" : "000000";
    const string newPassword = "NewPass8";
    Check(await reset.VerifyAsync(new("reader@example.invalid", wrongCode), default) is null, "Invalid reset code rejected");
    Check(!await reset.ResetAsync(new("reader@example.invalid", Crypto.NewToken(), newPassword), default), "Cannot skip OTP verification");
    var resetToken = await reset.VerifyAsync(new("reader@example.invalid", code), default);
    Check(resetToken is not null, "Valid OTP grants password reset");
    Check(await reset.VerifyAsync(new("reader@example.invalid", code), default) is null, "OTP cannot be verified twice");
    Check(!await reset.ResetAsync(new("locked@example.invalid", resetToken!, newPassword), default), "Token is bound to its account");
    Check(await reset.ResetAsync(new("reader@example.invalid", resetToken!, newPassword), default), "Valid grant changes password");
    Check(!await reset.ResetAsync(new("reader@example.invalid", resetToken!, newPassword), default), "Reset grant cannot be replayed");
    Check(await auth.CurrentAsync(oldSession!.Value.Token, default) is null, "Reset revokes existing sessions");
    Check(await auth.LoginAsync("reader@example.invalid", password, default) is null, "Old password no longer works");
    Check(await auth.LoginAsync("reader@example.invalid", newPassword, default) is not null, "New password works");
    await reset.RequestAsync("reader@example.invalid", default);
    code = transport.Code;
    wrongCode = code == "000000" ? "000001" : "000000";
    for (var i = 0; i < 5; i++) Check(await reset.VerifyAsync(new("reader@example.invalid", wrongCode), default) is null, "Invalid reset attempt");
    Check(await reset.VerifyAsync(new("reader@example.invalid", code), default) is null, "Reset locked after five failures");
    await store.PasswordResets.UpdateManyAsync(_ => true, Builders<PaperFlow.Api.Models.EmailOtpRecord>.Update.Set(x => x.CreatedAt, DateTime.UtcNow.AddMinutes(-2)));
    await reset.RequestAsync("reader@example.invalid", default);
    code = transport.Code;
    await store.PasswordResets.UpdateManyAsync(_ => true, Builders<PaperFlow.Api.Models.EmailOtpRecord>.Update.Set(x => x.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
    Check(await reset.VerifyAsync(new("reader@example.invalid", code), default) is null, "Expired reset code rejected");
    await store.PasswordResets.UpdateManyAsync(_ => true, Builders<PaperFlow.Api.Models.EmailOtpRecord>.Update.Set(x => x.CreatedAt, DateTime.UtcNow.AddMinutes(-2)));
    await reset.RequestAsync("reader@example.invalid", default);
    resetToken = await reset.VerifyAsync(new("reader@example.invalid", transport.Code), default);
    await store.PasswordResets.UpdateManyAsync(_ => true, Builders<PaperFlow.Api.Models.EmailOtpRecord>.Update.Set(x => x.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
    Check(!await reset.ResetAsync(new("reader@example.invalid", resetToken!, password), default), "Expired grant rejected");
    foreach (var requestType in new[] { typeof(RegisterRequest), typeof(ResetPasswordRequest) })
    {
        // ASP.NET uses positional-record constructor metadata for validation.
        var rules = requestType.GetConstructors().First().GetParameters().First(x => x.Name == "Password")
            .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute), true)
            .Cast<System.ComponentModel.DataAnnotations.ValidationAttribute>().ToArray();
        Check(rules.All(rule => rule.IsValid("12345678")), "Eight-character password accepted");
        Check(rules.Any(rule => !rule.IsValid("1234567")), "Seven-character password rejected");
    }
    transport.FailDelivery = true;
    try
    {
        await auth.RegisterAsync(new("delivery-failure@example.invalid", "Reader", password), default);
        throw new Exception("Delivery failure must propagate");
    }
    catch (InvalidOperationException) { }
    var failedUser = await store.Users.Find(x => x.Email == "delivery-failure@example.invalid").FirstAsync();
    Check(await store.Otps.CountDocumentsAsync(x => x.UserId == failedUser.Id) == 0, "Unsent registration OTP is removed");
    transport.FailDelivery = false;
    await auth.ResendOtpAsync(failedUser.Email, default);
    Check(await store.Otps.CountDocumentsAsync(x => x.UserId == failedUser.Id) == 1, "Retry after delivery failure works immediately");
    Console.WriteLine("Auth checks passed, including delivery failure retry, password reset, cooldown, expiry, attempt limits, replay protection and session revocation.");
}
finally
{
    if (!databaseName.StartsWith("PaperFlowAuthTests_", StringComparison.Ordinal)) throw new Exception("Unexpected test database");
    await client.DropDatabaseAsync(databaseName);
}

sealed class FakeMail(IConfiguration config) : IAuthEmailService
{
    public string Code { get; private set; } = "";
    public int Sends { get; private set; }
    public bool FailDelivery { get; set; }
    public void ValidateConfiguration() => new GmailEmailService(config).ValidateConfiguration();
    public Task SendOtpAsync(string email, string code, CancellationToken ct)
    {
        ValidateConfiguration();
        if (FailDelivery) throw new InvalidOperationException("Fake delivery failure");
        Code = code; Sends++; return Task.CompletedTask;
    }
    public Task SendPasswordResetAsync(string email, string code, CancellationToken ct) => SendOtpAsync(email, code, ct);
}

sealed class FakeBrevoHttp : HttpMessageHandler
{
    public int Status { get; set; } = 201;
    public string Body { get; private set; } = "";
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.RequestUri?.AbsoluteUri != "https://api.brevo.com/v3/smtp/email" || !request.Headers.Contains("api-key"))
            throw new Exception("Invalid Brevo endpoint or authentication");
        Body = await request.Content!.ReadAsStringAsync(ct);
        return new HttpResponseMessage((System.Net.HttpStatusCode)Status) { Content = new StringContent("{}") };
    }
}
