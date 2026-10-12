using System.Net.Http.Json;
using MimeKit;

namespace PaperFlow.Api.Services;

public class BrevoEmailService(HttpClient client, IConfiguration config, ILogger<BrevoEmailService>? logger = null) : IAuthEmailService
{
    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(config["Brevo:ApiKey"])) throw new AuthConfigurationException("Missing Brevo:ApiKey");
        if (!MailboxAddress.TryParse(config["Brevo:SenderEmail"], out _)) throw new AuthConfigurationException("Invalid Brevo:SenderEmail");
    }

    public Task SendOtpAsync(string email, string code, CancellationToken ct) => SendAsync(email,
        "PaperFlow - Xác minh email", $"Mã xác minh PaperFlow của bạn: {code}. Mã có hiệu lực trong 5 phút. Không chia sẻ mã này.", ct);
    public Task SendPasswordResetAsync(string email, string code, CancellationToken ct) => SendAsync(email,
        "PaperFlow - Đặt lại mật khẩu", $"Mã đặt lại mật khẩu PaperFlow: {code}. Mã có hiệu lực trong 5 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này. Không chia sẻ mã này.", ct);

    private async Task SendAsync(string email, string subject, string text, CancellationToken ct)
    {
        ValidateConfiguration();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", config["Brevo:ApiKey"]!.Trim());
        request.Content = JsonContent.Create(new {
            sender = new { name = "PaperFlow", email = MailboxAddress.Parse(config["Brevo:SenderEmail"]!).Address },
            to = new[] { new { email } }, subject, textContent = text
        });
        try
        {
            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                // Do not log provider response bodies: they may contain recipient details.
                logger?.LogWarning("Brevo email rejected. HTTP status: {Status}", (int)response.StatusCode);
                throw new InvalidOperationException("Brevo rejected email delivery.");
            }
            logger?.LogInformation("Brevo accepted an authentication email for delivery.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            logger?.LogWarning("Brevo connection failed. Error type: {ErrorType}", ex.GetType().Name);
            throw new InvalidOperationException("Unable to send email via Brevo HTTPS API.");
        }
    }
}
