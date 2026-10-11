
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PaperFlow.Api.Models;

public class AiHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Type { get; set; } = "translate";

    public string Input { get; set; } = "";

    public string Output { get; set; } = "";

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;
}
