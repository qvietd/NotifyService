using NotifyService.Domain.Entities;

namespace NotifyService.Infrastructure.Repositories;

public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(string notificationId);
    Task<int> GetUnreadCountAsync(string userId);
    Task<bool> UpdateAsync(Notification notification);
}
