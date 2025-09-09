using NotifyService.Shared.Models;

namespace NotifyService.Application.Interfaces;
public interface INotificationService
{
    Task<NotificationMessage> CreateOrUpdateNotificationAsync(NotificationRequest request);
    Task<IEnumerable<NotificationMessage>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20);
    Task<bool> MarkAsReadAsync(string notificationId);
    Task<int> GetUnreadCountAsync(string userId);
    Task BatchProcessNotificationsAsync(List<NotificationRequest> notifications);
}