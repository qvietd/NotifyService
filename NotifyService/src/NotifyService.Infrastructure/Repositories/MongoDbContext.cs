using MongoDB.Driver;
using NotifyService.NotifyService.Core.Entities;

namespace NotifyService.NotifyService.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB")
            ?? throw new ArgumentNullException("MongoDB connection string is missing");

        var databaseName = configuration["MongoDB:DatabaseName"] ?? "NotifyDB";

        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase(databaseName);

        // Create indexes
        CreateIndexes();
    }

    public IMongoCollection<Notification> Notifications =>
        _database.GetCollection<Notification>("notifications");

    private void CreateIndexes()
    {
        var indexKeysDefinition = Builders<Notification>.IndexKeys
            .Ascending(x => x.UserId)
            .Descending(x => x.CreatedAt);

        var indexModel = new CreateIndexModel<Notification>(
            indexKeysDefinition,
            new CreateIndexOptions { Name = "userId_createdAt" });

        Notifications.Indexes.CreateOne(indexModel);

        // Index for status
        var statusIndex = Builders<Notification>.IndexKeys.Ascending(x => x.Status);
        Notifications.Indexes.CreateOne(new CreateIndexModel<Notification>(
            statusIndex,
            new CreateIndexOptions { Name = "status" }));
    }
}