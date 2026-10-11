using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PaperFlow.Api.Services;

public class ResendEmailService(HttpClient client, IConfiguration config)
{
    public async Task SendOtpAsync(string email, string code, CancellationToken ct)
    {
        string apiKey = config["Resend:ApiKey"] ?? throw new InvalidOperationException("Missing Resend:ApiKey");
        string from = config["Resend:From"] ?? throw new InvalidOperationException("Missing Resend:From");
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = JsonContent.Create(new {
            from, to = new[] { email }, subject = "PaperFlow - Xác minh email",
            text = $"Mã xác minh PaperFlow của bạn: {code}. Mã có hiệu lực trong 5 phút. Không chia sẻ mã này."
        });
        using var response = await client.SendAsync(req, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Resend HTTP {(int)response.StatusCode}");
    }
}
