using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotifyService.Domain.Entities;
using NotifyService.Infrastructure.Configuration;

namespace NotifyService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{

    private readonly IMongoCollection<NotificationMessage> _collection;
    private readonly ILogger<NotificationRepository> _logger;
    private readonly MongoDBConfig _config;

    public NotificationRepository(
        IOptions<MongoDBConfig> config,
        ILogger<NotificationRepository> logger)
    {
        _config = config.Value;
        _logger = logger;

        var client = new MongoClient(_config.ConnectionString);
        var database = client.GetDatabase(_config.DatabaseName);
        _collection = database.GetCollection<NotificationMessage>(_config.CollectionName);
    }

    public async Task<bool> BatchInsertAsync(IEnumerable<NotificationMessage> messages)
    {
        try
        {
            var messageList = messages.ToList();
            if (!messageList.Any()) return true;

            await _collection.InsertManyAsync(messageList, new InsertManyOptions
            {
                IsOrdered = false
            });

            _logger.LogInformation($"Batch inserted {messageList.Count} messages");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to batch insert messages");
            return false;
        }
    }

    public async Task<IEnumerable<NotificationMessage>> GetPendingMessagesAsync(int limit)
    {
        var filter = Builders<NotificationMessage>.Filter.Eq(x => x.Status, NotificationStatus.Pending);
        var sort = Builders<NotificationMessage>.Sort.Ascending(x => x.CreatedAt);

        return await _collection
            .Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<bool> UpdateMessageStatusAsync(string messageId, NotificationStatus status, string error = null)
    {
        var filter = Builders<NotificationMessage>.Filter.Eq(x => x.MessageId, messageId);
        var update = Builders<NotificationMessage>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        if (!string.IsNullOrEmpty(error))
        {
            update = update.Set(x => x.ErrorMessage, error);
        }

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> UpdateBatchStatusAsync(IEnumerable<string> messageIds, NotificationStatus status)
    {
        var filter = Builders<NotificationMessage>.Filter.In(x => x.MessageId, messageIds);
        var update = Builders<NotificationMessage>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await _collection.UpdateManyAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<NotificationMessage> GetMessageByIdAsync(string messageId)
    {
        var filter = Builders<NotificationMessage>.Filter.Eq(x => x.MessageId, messageId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<NotificationMessage>> GetFailedMessagesForRetryAsync()
    {
        var filter = Builders<NotificationMessage>.Filter.And(
            Builders<NotificationMessage>.Filter.Eq(x => x.Status, NotificationStatus.Failed),
            Builders<NotificationMessage>.Filter.Lte(x => x.NextRetryAt, DateTime.UtcNow),
            Builders<NotificationMessage>.Filter.Lt(x => x.RetryCount, 5)
        );

        return await _collection.Find(filter).ToListAsync();
    }

    // public async Task<List<NotificationMessage>> GetUserNotificationsAsync(string userId, int page, int pageSize)
    // {
    //     var filter = Builders<NotificationMessage>.Filter.Eq(n => n.UserId, userId);
    //     var sort = Builders<NotificationMessage>.Sort.Descending(n => n.UpdatedAt);

    //     return await _collection.Find(filter)
    //         .Sort(sort)
    //         .Skip((page - 1) * pageSize)
    //         .Limit(pageSize)
    //         .ToListAsync();
    // }
}
