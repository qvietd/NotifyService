using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotifyService.Domain.Entities;
using NotifyService.Infrastructure.Configuration;

namespace NotifyService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{

    private readonly IMongoCollection<NotificationMessage> _collection;
    private readonly MongoDBSettings _settings;

    public NotificationRepository(IMongoDatabase database, IOptions<MongoDBSettings> settings)
    {
        _settings = settings.Value;
        _collection = database.GetCollection<NotificationMessage>(_settings.CollectionName);
    }

    public async Task BatchInsertAsync(IEnumerable<NotificationMessage> notifications)
    {
        if (notifications?.Any() == true)
        {
            await _collection.InsertManyAsync(notifications);
        }
    }

    public async Task UpdateStatusAsync(string id, NotificationStatus status, string errorMessage = null)
    {
        var filter = Builders<NotificationMessage>.Filter.Eq(x => x.Id, id);
        var update = Builders<NotificationMessage>.Update
            .Set(x => x.Status, status)
            .Set(x => x.ProcessedAt, DateTime.UtcNow);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            update = update.Set(x => x.ErrorMessage, errorMessage);
        }

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task<NotificationMessage> GetByIdAsync(string id)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task IncrementRetryCountAsync(string id)
    {
        var filter = Builders<NotificationMessage>.Filter.Eq(x => x.Id, id);
        var update = Builders<NotificationMessage>.Update.Inc(x => x.RetryCount, 1);
        await _collection.UpdateOneAsync(filter, update);
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
