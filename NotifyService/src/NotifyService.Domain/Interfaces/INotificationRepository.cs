using NotifyService.Domain.Entities;

namespace NotifyService.Infrastructure.Repositories;

public interface INotificationRepository
{
    Task BatchInsertAsync(IEnumerable<NotificationMessage> notifications);
    Task UpdateStatusAsync(string id, NotificationStatus status, string errorMessage = null);
    Task<NotificationMessage> GetByIdAsync(string id);
    Task IncrementRetryCountAsync(string id);
    Task<IEnumerable<NotificationMessage>> GetPendingNotificationsAsync(int batchSize = 100);
}