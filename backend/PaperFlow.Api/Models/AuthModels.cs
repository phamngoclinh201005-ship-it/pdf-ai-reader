using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PaperFlow.Api.Models;

public class UserRecord
{
    [BsonId] public ObjectId Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class EmailOtpRecord
{
    [BsonId] public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string CodeHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class SessionRecord
{
    [BsonId] public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}
