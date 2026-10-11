using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PaperFlow.Api.Services;

public class GmailEmailService(IConfiguration config)
{
    public Task SendOtpAsync(
        string email,
        string code,
        CancellationToken ct)
    {
        return SendAsync(
            email,
            "PaperFlow - Xác minh email",
            $"Mã xác minh PaperFlow của bạn: {code}. " +
            "Mã có hiệu lực trong 5 phút. Không chia sẻ mã này.",
            ct
        );
    }

    public Task SendPasswordResetAsync(
        string email,
        string code,
        CancellationToken ct)
    {
        return SendAsync(
            email,
            "PaperFlow - Đặt lại mật khẩu",
            $"Mã đặt lại mật khẩu PaperFlow: {code}. " +
            "Mã có hiệu lực trong 5 phút.",
            ct
        );
    }

    private async Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken ct)
    {
        var host = config["Smtp:Host"] ?? "smtp.gmail.com";
        var port = int.Parse(config["Smtp:Port"] ?? "587");
        var username = config["Smtp:Username"]
            ?? throw new InvalidOperationException("Missing Smtp:Username");
        var password = config["Smtp:Password"]
            ?? throw new InvalidOperationException("Missing Smtp:Password");
        var from = config["Smtp:From"] ?? username;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("PaperFlow", from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            host, port, SecureSocketOptions.StartTls, ct);

        await client.AuthenticateAsync(username, password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}