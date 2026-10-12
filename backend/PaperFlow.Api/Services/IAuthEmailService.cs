namespace PaperFlow.Api.Services;

public interface IAuthEmailService
{
    void ValidateConfiguration();
    Task SendOtpAsync(string email, string code, CancellationToken ct);
    Task SendPasswordResetAsync(string email, string code, CancellationToken ct);
}
