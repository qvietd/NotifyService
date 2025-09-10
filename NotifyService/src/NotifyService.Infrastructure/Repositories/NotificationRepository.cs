using MongoDB.Driver;
using NotifyService.Domain.Entities;

namespace NotifyService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<NotificationMessage> _collection;

    public NotificationRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<NotificationMessage>("notifications");
    }

    public async Task<NotificationMessage> CreateAsync(NotificationMessage notification)
    {
        await _collection.InsertOneAsync(notification);
        return notification;
    }

    public async Task InsertManyAsync(IEnumerable<NotificationMessage> notifications)
    {
        await _collection.InsertManyAsync(notifications);
    }

    public async Task<List<NotificationMessage>> GetUserNotificationsAsync(string userId, int page, int pageSize)
    {
        var filter = Builders<NotificationMessage>.Filter.Eq(n => n.UserId, userId);
        var sort = Builders<NotificationMessage>.Sort.Descending(n => n.UpdatedAt);

        return await _collection.Find(filter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(string notificationId)
    {
        var filter = Builders<NotificationMessage>.Filter.Eq(n => n.Id, notificationId);
        var update = Builders<NotificationMessage>.Update.Set(n => n.IsRead, true);

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        var filter = Builders<NotificationMessage>.Filter.And(
            Builders<NotificationMessage>.Filter.Eq(n => n.UserId, userId),
            Builders<NotificationMessage>.Filter.Eq(n => n.IsRead, false)
        );

        return (int)await _collection.CountDocumentsAsync(filter);
    }

    public async Task<bool> UpdateAsync(NotificationMessage notification)
    {
        var filter = Builders<NotificationMessage>.Filter.Eq(n => n.Id, notification.Id);
        var result = await _collection.ReplaceOneAsync(filter, notification);
        return result.ModifiedCount > 0;
    }

    public async Task<List<NotificationMessage>> GetByIdsAsync(List<string> notificationIds)
    {
        var filter = Builders<NotificationMessage>.Filter.In(n => n.Id, notificationIds);
        return await _collection.Find(filter).ToListAsync();
    }
}
