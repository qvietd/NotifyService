using MongoDB.Driver;
using NotifyService.Domain.Entities;

namespace NotifyService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<Notification> _collection;

    public NotificationRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Notification>("notifications");
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        await _collection.InsertOneAsync(notification);
        return notification;
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.UserId, userId);
        var sort = Builders<Notification>.Sort.Descending(n => n.CreatedAt);

        return await _collection.Find(filter)
            .Sort(sort)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(string notificationId)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.Id, notificationId);
        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        var filter = Builders<Notification>.Filter.And(
            Builders<Notification>.Filter.Eq(n => n.UserId, userId),
            Builders<Notification>.Filter.Eq(n => n.IsRead, false)
        );

        return (int)await _collection.CountDocumentsAsync(filter);
    }

    public async Task<bool> UpdateAsync(Notification notification)
    {
        var filter = Builders<Notification>.Filter.Eq(n => n.Id, notification.Id);
        var result = await _collection.ReplaceOneAsync(filter, notification);
        return result.ModifiedCount > 0;
    }
}
