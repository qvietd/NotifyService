using NotifyService.Shared.Models;

namespace NotifyService.Application.Interfaces;
public interface INotificationProcessor
{
    Task ProcessNotificationAsync(NotificationRequest request);
    Task ProcessBatchNotificationsAsync(List<NotificationRequest> requests);
}