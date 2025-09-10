using NotifyService.Domain.Entities;

namespace NotifyService.Infrastructure.Repositories;

public interface INotificationRepository
{
    Task<NotificationMessage> CreateAsync(NotificationMessage notification);
    Task<List<NotificationMessage>> CreateBulkAsync(List<NotificationMessage> notifications);
    Task<NotificationMessage> UpdateAsync(NotificationMessage notification);
    Task<List<NotificationMessage>> GetPendingNotificationsAsync(int batchSize = 100);
    Task<OutboxEvent> CreateOutboxEventAsync(OutboxEvent outboxEvent);
    Task<List<OutboxEvent>> GetUnprocessedOutboxEventsAsync(int batchSize = 100);
    Task UpdateOutboxEventAsync(OutboxEvent outboxEvent);
}