using System.Security.Cryptography;
using MongoDB.Bson;
using MongoDB.Driver;
using PaperFlow.Api.Models;
using PaperFlow.Api.Contracts;
using PaperFlow.Api.Security;

namespace PaperFlow.Api.Services;

public class AuthService(AuthStore db, IConfiguration config, IAuthEmailService mail)
{
    private string OtpSecret
    {
        get
        {
            var secret = config["Auth:OtpSecret"];
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
                throw new AuthConfigurationException("Auth:OtpSecret must contain at least 32 characters");
            return secret;
        }
    }
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
    public static PublicUser Public(UserRecord u) => new(u.Id.ToString(), u.Email, u.DisplayName, u.Role);

    private OtpTiming GenericTiming()
    {
        var now = DateTime.UtcNow;
        return new(now.AddMinutes(config.GetValue("Auth:OtpMinutes", 5)), now.AddSeconds(60), now);
    }

    public async Task<OtpTiming> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        _ = OtpSecret;
        mail.ValidateConfiguration();
        string email = Normalize(request.Email);
        var existing = await db.Users.Find(x => x.Email == email).FirstOrDefaultAsync(ct);
        if (existing is { EmailVerified: true }) return GenericTiming(); // Keep the response shape uniform.
        UserRecord user;
        if (existing is null)
        {
            user = new UserRecord { Email = email, DisplayName = request.DisplayName.Trim(), PasswordHash = Crypto.HashPassword(request.Password) };
            try { await db.Users.InsertOneAsync(user, cancellationToken: ct); }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey) { return GenericTiming(); }
        }
        else user = existing; // Never overwrite password on re-registration
        return await SendOtpAsync(user, ct);
    }

    public async Task<OtpTiming> ResendOtpAsync(string email, CancellationToken ct)
    {
        _ = OtpSecret;
        mail.ValidateConfiguration();
        var user = await db.Users.Find(x => x.Email == Normalize(email)).FirstOrDefaultAsync(ct);
        return user is { EmailVerified: false } ? await SendOtpAsync(user, ct) : GenericTiming();
    }

    private async Task<OtpTiming> SendOtpAsync(UserRecord user, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        string code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var update = Builders<EmailOtpRecord>.Update
            .Set(x => x.CodeHash, Crypto.OtpHash(OtpSecret, user.Email, code))
            .Set(x => x.ExpiresAt, now.AddMinutes(config.GetValue("Auth:OtpMinutes", 5)))
            .Set(x => x.CreatedAt, now).Set(x => x.Attempts, 0)
            .SetOnInsert(x => x.UserId, user.Id);
        try
        {
            // The unique UserId index prevents concurrent resend requests from bypassing cooldown.
            await db.Otps.UpdateOneAsync(x => x.UserId == user.Id && x.CreatedAt <= now.AddSeconds(-60),
                update, new UpdateOptions { IsUpsert = true }, ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var existing = await db.Otps.Find(x => x.UserId == user.Id).FirstOrDefaultAsync(ct);
            return existing is null ? GenericTiming() : new(existing.ExpiresAt, existing.CreatedAt.AddSeconds(60), DateTime.UtcNow);
        }
        try { await mail.SendOtpAsync(user.Email, code, ct); }
        catch
        {
            // A failed delivery must not leave an unsent OTP blocking retry for 60 seconds.
            await db.Otps.DeleteOneAsync(x => x.UserId == user.Id && x.CodeHash == Crypto.OtpHash(OtpSecret, user.Email, code), CancellationToken.None);
            throw;
        }
        return new(now.AddMinutes(config.GetValue("Auth:OtpMinutes", 5)), now.AddSeconds(60), DateTime.UtcNow);
    }

    public async Task<bool> VerifyAsync(string email, string code, CancellationToken ct)
    {
        var user = await db.Users.Find(x => x.Email == Normalize(email)).FirstOrDefaultAsync(ct);
        if (user is null) return false;
        if (user.EmailVerified) return false;
        var otp = await db.Otps.FindOneAndUpdateAsync(
            x => x.UserId == user.Id && x.ExpiresAt > DateTime.UtcNow && x.Attempts < 5,
            Builders<EmailOtpRecord>.Update.Inc(x => x.Attempts, 1),
            new FindOneAndUpdateOptions<EmailOtpRecord> { ReturnDocument = ReturnDocument.Before }, ct);
        if (otp is null) return false;
        var expected = Convert.FromHexString(otp.CodeHash);
        var actual = Convert.FromHexString(Crypto.OtpHash(OtpSecret, user.Email, code));
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            return false;
        }
        var consumed = await db.Otps.DeleteOneAsync(x => x.Id == otp.Id && x.CodeHash == otp.CodeHash, ct);
        if (consumed.DeletedCount != 1) return false;
        await db.Users.UpdateOneAsync(x => x.Id == user.Id, Builders<UserRecord>.Update.Set(x => x.EmailVerified, true), cancellationToken: ct);
        return true;
    }

    public async Task<(string Token, UserRecord User)?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await db.Users.Find(x => x.Email == Normalize(email)).FirstOrDefaultAsync(ct);
        if (user is null || !Crypto.VerifyPassword(password, user.PasswordHash) || !user.EmailVerified) return null;
        var token = Crypto.NewToken();
        var days = config.GetValue("Auth:SessionDays", 7);
        await db.Sessions.InsertOneAsync(new SessionRecord { UserId = user.Id, TokenHash = Crypto.Sha256(token), ExpiresAt = DateTime.UtcNow.AddDays(days) }, cancellationToken: ct);
        return (token, user);
    }

    public async Task<UserRecord?> CurrentAsync(string? token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var session = await db.Sessions.Find(x => x.TokenHash == Crypto.Sha256(token) && x.ExpiresAt > DateTime.UtcNow).FirstOrDefaultAsync(ct);
        if (session is null) return null;
        return await db.Users.Find(x => x.Id == session.UserId && x.EmailVerified).FirstOrDefaultAsync(ct);
    }

    public Task LogoutAsync(string? token, CancellationToken ct) => string.IsNullOrWhiteSpace(token)
        ? Task.CompletedTask
        : db.Sessions.DeleteOneAsync(x => x.TokenHash == Crypto.Sha256(token), ct);
}
