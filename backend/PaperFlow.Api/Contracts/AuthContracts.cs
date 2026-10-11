using System.ComponentModel.DataAnnotations;

namespace PaperFlow.Api.Contracts;

public record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(2), MaxLength(80)] string DisplayName, [Required, MinLength(12), MaxLength(128)] string Password);
public record VerifyRequest([Required, EmailAddress] string Email, [Required, RegularExpression("^[0-9]{6}$")] string Code);
public record LoginRequest([Required, EmailAddress, MaxLength(254)] string Email, [Required, MaxLength(128)] string Password);
public record ResendOtpRequest([Required, EmailAddress] string Email);
public record PublicUser(string Id, string Email, string DisplayName, string Role);
public record OtpTiming(DateTime ExpiresAt, DateTime ResendAvailableAt, DateTime ServerTime);
