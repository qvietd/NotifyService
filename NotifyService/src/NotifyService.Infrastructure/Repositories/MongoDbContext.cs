using MongoDB.Driver;
using NotifyService.Domain.Entities;

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

    public IMongoCollection<NotificationMessage> Notifications =>
        _database.GetCollection<NotificationMessage>("notifications");

    private void CreateIndexes()
    {
        var indexKeysDefinition = Builders<NotificationMessage>.IndexKeys
            .Ascending(x => x.UserId)
            .Descending(x => x.CreatedAt);

        var indexModel = new CreateIndexModel<NotificationMessage>(
            indexKeysDefinition,
            new CreateIndexOptions { Name = "userId_createdAt" });

        Notifications.Indexes.CreateOne(indexModel);

        // Index for status
        var statusIndex = Builders<NotificationMessage>.IndexKeys.Ascending(x => x.Status);
        Notifications.Indexes.CreateOne(new CreateIndexModel<NotificationMessage>(
            statusIndex,
            new CreateIndexOptions { Name = "status" }));
    }
}