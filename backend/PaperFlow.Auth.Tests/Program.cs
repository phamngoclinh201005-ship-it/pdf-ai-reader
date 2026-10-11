using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    ["Resend:ApiKey"] = "fake-test-key",
    ["Resend:From"] = "test@example.invalid"
}).Build();
var client = new MongoClient(mongoUri);
var store = new AuthStore(config);
var transport = new FakeMail();
var auth = new AuthService(store, config, new ResendEmailService(new HttpClient(transport), config));
void Check(bool condition, string label) { if (!condition) throw new Exception(label); }
const string password = "StrongTestPassword123!";
try
{
    await store.EnsureIndexesAsync();
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
    Console.WriteLine("Auth integration checks passed: registration, cooldown, OTP validation/replay/lockout, login, session hashing, logout.");
}
finally
{
    if (!databaseName.StartsWith("PaperFlowAuthTests_", StringComparison.Ordinal)) throw new Exception("Unexpected test database");
    await client.DropDatabaseAsync(databaseName);
}

sealed class FakeMail : HttpMessageHandler
{
    public string Code { get; private set; } = "";
    public int Sends { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        Code = Regex.Match(body.RootElement.GetProperty("text").GetString()!, @"\b\d{6}\b").Value;
        Sends++;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
    }
}
