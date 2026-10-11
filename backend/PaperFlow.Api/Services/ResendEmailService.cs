using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PaperFlow.Api.Services;

public class ResendEmailService(HttpClient client, IConfiguration config)
{
    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(config["Resend:ApiKey"]))
            throw new AuthConfigurationException("Missing Resend:ApiKey");
        if (string.IsNullOrWhiteSpace(config["Resend:From"]))
            throw new AuthConfigurationException("Missing Resend:From");
    }

    public Task SendOtpAsync(string email, string code, CancellationToken ct) => SendOtpAsync(email, code, ct, false);
    public Task SendPasswordResetAsync(string email, string code, CancellationToken ct) => SendOtpAsync(email, code, ct, true);
    private async Task SendOtpAsync(string email, string code, CancellationToken ct, bool passwordReset)
    {
        ValidateConfiguration();
        string apiKey = config["Resend:ApiKey"] ?? throw new InvalidOperationException("Missing Resend:ApiKey");
        string from = config["Resend:From"] ?? throw new InvalidOperationException("Missing Resend:From");
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        if (passwordReset)
        {
            req.Content = JsonContent.Create(new {
                from, to = new[] { email }, subject = "PaperFlow - Đặt lại mật khẩu",
                text = $"Mã đặt lại mật khẩu PaperFlow: {code}. Mã có hiệu lực trong 5 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này. Không chia sẻ mã này."
            });
        }
        else req.Content = JsonContent.Create(new {
            from, to = new[] { email }, subject = "PaperFlow - Xác minh email",
            text = $"Mã xác minh PaperFlow của bạn: {code}. Mã có hiệu lực trong 5 phút. Không chia sẻ mã này."
        });
        using var response = await client.SendAsync(req, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Resend HTTP {(int)response.StatusCode}");
    }
}
