using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PaperFlow.Api.Contracts;
using PaperFlow.Api.Services;

namespace PaperFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class PasswordResetController(PasswordResetService reset, ILogger<PasswordResetController> logger) : ControllerBase
{
    [HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot(ForgotPasswordRequest request, CancellationToken ct)
    {
        try
        {
            var timing = await reset.RequestAsync(request.Email, ct);
            return Accepted(new { message = "Nếu email có tài khoản đã xác minh, mã đặt lại mật khẩu sẽ được gửi.", timing });
        }
        catch (AuthConfigurationException ex) { return Unavailable(ex); }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException || ex is OperationCanceledException && !ct.IsCancellationRequested)
        { return StatusCode(502, new { message = "Không gửi được email đặt lại mật khẩu. Vui lòng thử lại sau." }); }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> Reset(ResetPasswordRequest request, CancellationToken ct)
    {
        try
        {
            return await reset.ResetAsync(request, ct)
                ? Ok(new { message = "Đã đổi mật khẩu. Hãy đăng nhập bằng mật khẩu mới." })
                : BadRequest(new { message = "Mã không hợp lệ, đã hết hạn hoặc vượt quá số lần thử." });
        }
        catch (AuthConfigurationException ex) { return Unavailable(ex); }
    }

    [HttpPost("verify-password-reset")]
    public async Task<IActionResult> Verify(VerifyRequest request, CancellationToken ct)
    {
        try
        {
            var token = await reset.VerifyAsync(request, ct);
            return token is null ? BadRequest(new { message = "Mã không hợp lệ, đã hết hạn hoặc vượt quá số lần thử." }) : Ok(new { token });
        }
        catch (AuthConfigurationException ex) { return Unavailable(ex); }
    }

    private IActionResult Unavailable(AuthConfigurationException error)
    {
        logger.LogError("Password reset configuration unavailable: {Reason}", error.Message);
        return StatusCode(503, new { message = "Máy chủ chưa cấu hình dịch vụ email. Vui lòng thử lại sau." });
    }
}
