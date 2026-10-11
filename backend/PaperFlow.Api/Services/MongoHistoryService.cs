
using MongoDB.Driver;
using PaperFlow.Api.Models;

namespace PaperFlow.Api.Services;

public class MongoHistoryService
{
    private readonly IMongoCollection<AiHistory> _collection;

    public MongoHistoryService(IConfiguration config)
    {
        var connectionString =
            config["MongoDB:ConnectionString"]
            ?? "mongodb://localhost:27017";

        var databaseName =
            config["MongoDB:DatabaseName"]
            ?? "PaperFlowDB";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        _collection =
            database.GetCollection<AiHistory>("ai_history");
    }

    public async Task SaveAsync(AiHistory history)
    {
        await _collection.InsertOneAsync(history);
    }

    public async Task<List<AiHistory>> GetRecentAsync()
    {
        return await _collection
            .Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .Limit(20)
            .ToListAsync();
    }
}
