using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PaperFlow.Api.Contracts;

namespace PaperFlow.Api.Models;

public class LibraryDocument
{
    [BsonId] public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ObjectId OwnerId { get; set; }
    public string Name { get; set; } = "";
    public long Size { get; set; }
    public int ChunkCount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ReadingState State { get; set; } = new();
}

public class LibraryChunk
{
    [BsonId] public ObjectId Id { get; set; }
    public string DocumentId { get; set; } = "";
    public ObjectId OwnerId { get; set; }
    public int Index { get; set; }
    public byte[] Data { get; set; } = [];
}
