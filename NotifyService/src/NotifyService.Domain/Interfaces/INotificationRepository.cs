using NotifyService.Domain.Entities;

namespace NotifyService.Infrastructure.Repositories;

public interface INotificationRepository
{
    Task<NotificationMessage> CreateAsync(NotificationMessage notification);
    Task<List<NotificationMessage>> GetUserNotificationsAsync(string userId, int page, int pageSize);
    Task<bool> MarkAsReadAsync(string notificationId);
    Task<int> GetUnreadCountAsync(string userId);
    Task<bool> UpdateAsync(NotificationMessage notification);
    Task<List<NotificationMessage>> GetByIdsAsync(List<string> notificationIds);
    Task InsertManyAsync(IEnumerable<NotificationMessage> notifications);
}