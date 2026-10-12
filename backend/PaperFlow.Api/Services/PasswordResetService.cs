using System.Security.Cryptography;
using MongoDB.Driver;
using PaperFlow.Api.Contracts;
using PaperFlow.Api.Models;
using PaperFlow.Api.Security;

namespace PaperFlow.Api.Services;

public class PasswordResetService(AuthStore db, IConfiguration config, IAuthEmailService mail)
{
    private string Secret => config["Auth:OtpSecret"] is { Length: >= 32 } value
        ? value : throw new AuthConfigurationException("Auth:OtpSecret must contain at least 32 characters");
    private string Hash(string email, string code) => Crypto.OtpHash(Secret, "password-reset:" + email, code);

    public async Task<OtpTiming> RequestAsync(string email, CancellationToken ct)
    {
        _ = Secret;
        mail.ValidateConfiguration();
        email = AuthService.Normalize(email);
        var now = DateTime.UtcNow;
        var timing = new OtpTiming(now.AddMinutes(5), now.AddSeconds(60), now);
        var user = await db.Users.Find(x => x.Email == email && x.EmailVerified).FirstOrDefaultAsync(ct);
        if (user is null) return timing;
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var hash = Hash(email, code);
        try
        {
            await db.PasswordResets.UpdateOneAsync(x => x.UserId == user.Id && x.CreatedAt <= now.AddSeconds(-60),
                Builders<EmailOtpRecord>.Update.Set(x => x.CodeHash, hash).Set(x => x.CreatedAt, now)
                    .Set(x => x.ExpiresAt, timing.ExpiresAt).Set(x => x.Attempts, 0).SetOnInsert(x => x.UserId, user.Id),
                new UpdateOptions { IsUpsert = true }, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey) { return timing; }
        try { await mail.SendPasswordResetAsync(email, code, ct); }
        catch
        {
            await db.PasswordResets.DeleteOneAsync(x => x.UserId == user.Id && x.CodeHash == hash, CancellationToken.None);
            throw;
        }
        return timing;
    }

    public async Task<string?> VerifyAsync(VerifyRequest request, CancellationToken ct)
    {
        var email = AuthService.Normalize(request.Email);
        var actual = Hash(email, request.Code);
        var user = await db.Users.Find(x => x.Email == email && x.EmailVerified).FirstOrDefaultAsync(ct);
        if (user is null) return null;
        var otp = await db.PasswordResets.FindOneAndUpdateAsync(
            x => x.UserId == user.Id && x.ExpiresAt > DateTime.UtcNow && x.Attempts < 5,
            Builders<EmailOtpRecord>.Update.Inc(x => x.Attempts, 1),
            new FindOneAndUpdateOptions<EmailOtpRecord> { ReturnDocument = ReturnDocument.Before }, ct);
        if (otp is null || !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(otp.CodeHash), Convert.FromHexString(actual))) return null;
        var token = Crypto.NewToken();
        var granted = await db.PasswordResets.UpdateOneAsync(x => x.Id == otp.Id && x.CodeHash == otp.CodeHash,
            Builders<EmailOtpRecord>.Update.Set(x => x.CodeHash, Crypto.Sha256("reset-grant:" + token)), cancellationToken: ct);
        return granted.ModifiedCount == 1 ? token : null;
    }

    public async Task<bool> ResetAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var email = AuthService.Normalize(request.Email);
        var user = await db.Users.Find(x => x.Email == email && x.EmailVerified).FirstOrDefaultAsync(ct);
        if (user is null) return false;
        var tokenHash = Crypto.Sha256("reset-grant:" + request.Token);
        var passwordHash = Crypto.HashPassword(request.Password);
        // Only one request can consume this code, even with concurrent submissions.
        var consumed = await db.PasswordResets.DeleteOneAsync(x => x.UserId == user.Id && x.CodeHash == tokenHash && x.ExpiresAt > DateTime.UtcNow, ct);
        if (consumed.DeletedCount != 1) return false;
        await db.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<UserRecord>.Update.Set(x => x.PasswordHash, passwordHash), cancellationToken: CancellationToken.None);
        await db.Sessions.DeleteManyAsync(x => x.UserId == user.Id, CancellationToken.None);
        return true;
    }
}
