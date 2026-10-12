using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PaperFlow.Api.Services;

public class GmailEmailService(IConfiguration config) : IAuthEmailService
{
    public void ValidateConfiguration()
    {
        foreach (var key in new[] { "Smtp:Username", "Smtp:Password" })
            if (string.IsNullOrWhiteSpace(config[key])) throw new AuthConfigurationException($"Missing {key}");
        if (config["Smtp:Host"] is { } host && string.IsNullOrWhiteSpace(host))
            throw new AuthConfigurationException("Invalid SMTP host");
        if (!MailboxAddress.TryParse(config["Smtp:Username"], out _) ||
            !MailboxAddress.TryParse(config["Smtp:From"] ?? config["Smtp:Username"], out _))
            throw new AuthConfigurationException("Invalid SMTP email address");
        if (config["Smtp:Port"] is { } port && (!int.TryParse(port, out var number) || number is not (587 or 465)))
            throw new AuthConfigurationException("SMTP port must be 587 or 465");
    }
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
        ValidateConfiguration();
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

        using var client = new SmtpClient { Timeout = 15000 };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            await client.ConnectAsync(host, port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, timeout.Token);
            await client.AuthenticateAsync(username, password.Replace(" ", ""), timeout.Token);
            await client.SendAsync(message, timeout.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is MailKit.CommandException or MailKit.ProtocolException or MailKit.Security.AuthenticationException or System.IO.IOException or System.Net.Sockets.SocketException or OperationCanceledException or System.Security.Authentication.AuthenticationException)
        {
            throw new InvalidOperationException("Unable to send email via Gmail SMTP.");
        }
    }
}
