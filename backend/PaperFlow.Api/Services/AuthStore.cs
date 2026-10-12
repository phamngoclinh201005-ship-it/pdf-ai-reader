using MongoDB.Bson;
using MongoDB.Driver;
using PaperFlow.Api.Models;

namespace PaperFlow.Api.Services;

public class AuthStore
{
    public IMongoCollection<UserRecord> Users { get; }
    public IMongoCollection<EmailOtpRecord> Otps { get; }
    public IMongoCollection<SessionRecord> Sessions { get; }
    public IMongoCollection<EmailOtpRecord> PasswordResets { get; }

    public AuthStore(IConfiguration config)
    {
        var client = new MongoClient(config["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017");
        var db = client.GetDatabase(config["MongoDB:DatabaseName"] ?? "PaperFlowDB");
        Users = db.GetCollection<UserRecord>("users");
        Otps = db.GetCollection<EmailOtpRecord>("email_otps");
        Sessions = db.GetCollection<SessionRecord>("sessions");
        PasswordResets = db.GetCollection<EmailOtpRecord>("password_resets");
    }
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await PasswordResets.Indexes.CreateOneAsync(new CreateIndexModel<EmailOtpRecord>(Builders<EmailOtpRecord>.IndexKeys.Ascending(x => x.UserId), new CreateIndexOptions { Unique = true }), cancellationToken: ct);
        await PasswordResets.Indexes.CreateOneAsync(new CreateIndexModel<EmailOtpRecord>(Builders<EmailOtpRecord>.IndexKeys.Ascending(x => x.ExpiresAt), new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }), cancellationToken: ct);
        await Users.Indexes.CreateOneAsync(new CreateIndexModel<UserRecord>(Builders<UserRecord>.IndexKeys.Ascending(x => x.Email), new CreateIndexOptions { Unique = true }), cancellationToken: ct);
        await Otps.Indexes.CreateOneAsync(new CreateIndexModel<EmailOtpRecord>(Builders<EmailOtpRecord>.IndexKeys.Ascending(x => x.UserId), new CreateIndexOptions { Unique = true }), cancellationToken: ct);
        await Otps.Indexes.CreateOneAsync(new CreateIndexModel<EmailOtpRecord>(Builders<EmailOtpRecord>.IndexKeys.Ascending(x => x.ExpiresAt), new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }), cancellationToken: ct);
        await Sessions.Indexes.CreateOneAsync(new CreateIndexModel<SessionRecord>(Builders<SessionRecord>.IndexKeys.Ascending(x => x.TokenHash), new CreateIndexOptions { Unique = true }), cancellationToken: ct);
        await Sessions.Indexes.CreateOneAsync(new CreateIndexModel<SessionRecord>(Builders<SessionRecord>.IndexKeys.Ascending(x => x.ExpiresAt), new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }), cancellationToken: ct);
    }
}
