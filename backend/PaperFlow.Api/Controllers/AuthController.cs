using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PaperFlow.Api.Models;
using PaperFlow.Api.Contracts;
using PaperFlow.Api.Services;

namespace PaperFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth, IConfiguration config, IWebHostEnvironment environment) : ControllerBase
{
    private string CookieName => config["Auth:CookieName"] ?? "paperflow_session";
    private CookieOptions CookieOptions() => new()
    {
        HttpOnly = true, Secure = !environment.IsDevelopment() || Request.IsHttps,
        SameSite = SameSiteMode.Lax, Path = "/", Expires = DateTimeOffset.UtcNow.AddDays(config.GetValue("Auth:SessionDays", 7))
    };

    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest body, CancellationToken ct)
    {
        try
        {
            var timing = await auth.RegisterAsync(body, ct);
            return Accepted(new { message = "Nếu email hợp lệ, mã xác minh sẽ được gửi.", timing });
        }
        catch (InvalidOperationException) { return StatusCode(502, new { message = "Không gửi được email xác minh. Kiểm tra cấu hình Resend." }); }
    }
    [EnableRateLimiting("auth")]
    [HttpPost("resend-otp")]
    public async Task<IActionResult> Resend(ResendOtpRequest body, CancellationToken ct)
    {
        try
        {
            var timing = await auth.ResendOtpAsync(body.Email, ct);
            return Accepted(new { message = "Nếu tài khoản cần xác minh, mã sẽ được gửi.", timing });
        }
        catch (InvalidOperationException) { return StatusCode(502, new { message = "Không gửi được email xác minh." }); }
    }
    [EnableRateLimiting("auth")]
    [HttpPost("verify-email")]
    public async Task<IActionResult> Verify(VerifyRequest body, CancellationToken ct) =>
        await auth.VerifyAsync(body.Email, body.Code, ct) ? Ok(new { message = "Xác minh thành công." }) : BadRequest(new { message = "Mã không hợp lệ hoặc đã hết hạn." });

    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest body, CancellationToken ct)
    {
        var result = await auth.LoginAsync(body.Email, body.Password, ct);
        if (result is null) return Unauthorized(new { message = "Email, mật khẩu không đúng hoặc email chưa xác minh." });
        Response.Cookies.Append(CookieName, result.Value.Token, CookieOptions());
        return Ok(new { user = AuthService.Public(result.Value.User) });
    }
    [EnableRateLimiting("auth")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(Request.Cookies[CookieName], ct);
        Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
        return Ok(new { message = "Đã đăng xuất." });
    }
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await auth.CurrentAsync(Request.Cookies[CookieName], ct);
        return user is null ? Unauthorized(new { message = "Chưa đăng nhập." }) : Ok(new { user = AuthService.Public(user) });
    }
    [HttpGet("admin-check")]
    public async Task<IActionResult> AdminCheck(CancellationToken ct)
    {
        var user = await auth.CurrentAsync(Request.Cookies[CookieName], ct);
        if (user is null) return Unauthorized();
        if (user.Role != "Admin") return StatusCode(403, new { message = "Admin access required" });
        return Ok(new { message = "Bạn có quyền Admin." });
    }
}
