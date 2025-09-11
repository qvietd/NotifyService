using NotifyService.Domain.Entities;

namespace NotifyService.Infrastructure.Repositories;

public interface INotificationRepository
{
    Task<bool> BatchInsertAsync(IEnumerable<NotificationMessage> messages);

    Task<IEnumerable<NotificationMessage>> GetPendingMessagesAsync(int limit);

    Task<bool> UpdateMessageStatusAsync(string messageId, NotificationStatus status, string error = null);

    Task<bool> UpdateBatchStatusAsync(IEnumerable<string> messageIds, NotificationStatus status);

    Task<NotificationMessage> GetMessageByIdAsync(string messageId);

    Task<IEnumerable<NotificationMessage>> GetFailedMessagesForRetryAsync();
}