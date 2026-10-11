using MongoDB.Bson;
using MongoDB.Driver;
using PaperFlow.Api.Contracts;
using PaperFlow.Api.Models;

namespace PaperFlow.Api.Services;

public class LibraryStore(IConfiguration config)
{
    private readonly IMongoDatabase database = new MongoClient(config["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017")
        .GetDatabase(config["MongoDB:DatabaseName"] ?? "PaperFlowDB");
    private IMongoCollection<LibraryDocument> Documents => database.GetCollection<LibraryDocument>("library_documents");
    private IMongoCollection<LibraryChunk> Chunks => database.GetCollection<LibraryChunk>("library_chunks");

    public async Task EnsureIndexesAsync()
    {
        await Documents.Indexes.CreateOneAsync(new CreateIndexModel<LibraryDocument>(Builders<LibraryDocument>.IndexKeys.Ascending(x => x.OwnerId).Descending(x => x.UpdatedAt)));
        await Chunks.Indexes.CreateOneAsync(new CreateIndexModel<LibraryChunk>(Builders<LibraryChunk>.IndexKeys.Ascending(x => x.DocumentId).Ascending(x => x.Index), new CreateIndexOptions { Unique = true }));
    }

    public async Task<List<LibraryItem>> ListAsync(ObjectId owner, CancellationToken ct) =>
        await Documents.Find(x => x.OwnerId == owner).SortByDescending(x => x.UpdatedAt).Limit(100)
            .Project(x => new LibraryItem(x.Id, x.Name, x.Size, x.UpdatedAt, x.State.Page)).ToListAsync(ct);

    public Task<LibraryDocument?> GetAsync(ObjectId owner, string id, CancellationToken ct) =>
        Documents.Find(x => x.OwnerId == owner && x.Id == id).FirstOrDefaultAsync(ct)!;

    public async Task<LibraryDocument> UploadAsync(ObjectId owner, string name, Stream stream, long size, CancellationToken ct)
    {
        var document = new LibraryDocument { OwnerId = owner, Name = name, Size = size };
        // Each chunk stays below MongoDB's BSON document limit, including 40 MB PDFs.
        var buffer = new byte[1024 * 1024];
        long total = 0;
        try
        {
            int read;
            while ((read = await stream.ReadAsync(buffer, ct)) != 0)
            {
                total += read;
                if (total > 40 * 1024 * 1024) throw new InvalidDataException("PDF exceeds 40 MB");
                await Chunks.InsertOneAsync(new LibraryChunk { DocumentId = document.Id, OwnerId = owner, Index = document.ChunkCount++, Data = buffer[..read] }, cancellationToken: ct);
            }
            if (total != size) throw new InvalidDataException("Incomplete PDF upload");
            await Documents.InsertOneAsync(document, cancellationToken: ct);
            return document;
        }
        catch
        {
            await Chunks.DeleteManyAsync(x => x.DocumentId == document.Id && x.OwnerId == owner, CancellationToken.None);
            throw;
        }
    }

    public async Task<byte[]?> ReadAsync(ObjectId owner, string id, CancellationToken ct)
    {
        var document = await GetAsync(owner, id, ct);
        if (document is null) return null;
        using var output = new MemoryStream((int)document.Size);
        using var cursor = await Chunks.Find(x => x.DocumentId == id && x.OwnerId == owner).SortBy(x => x.Index).ToCursorAsync(ct);
        var count = 0;
        while (await cursor.MoveNextAsync(ct)) foreach (var chunk in cursor.Current)
        {
            if (chunk.Index != count++) throw new InvalidDataException("Incomplete saved PDF");
            await output.WriteAsync(chunk.Data, ct);
        }
        if (count != document.ChunkCount || output.Length != document.Size) throw new InvalidDataException("Incomplete saved PDF");
        return output.ToArray();
    }

    public async Task<bool> UpdateAsync(ObjectId owner, string id, ReadingState state, CancellationToken ct)
    {
        var result = await Documents.UpdateOneAsync(x => x.OwnerId == owner && x.Id == id,
            Builders<LibraryDocument>.Update.Set(x => x.State, state).Set(x => x.UpdatedAt, DateTime.UtcNow), cancellationToken: ct);
        return result.MatchedCount == 1;
    }
    public async Task<bool> DeleteAsync(ObjectId owner, string id, CancellationToken ct)
    {
        var result = await Documents.DeleteOneAsync(x => x.OwnerId == owner && x.Id == id, ct);
        if (result.DeletedCount == 0) return false;
        await Chunks.DeleteManyAsync(x => x.OwnerId == owner && x.DocumentId == id, CancellationToken.None);
        return true;
    }
}
