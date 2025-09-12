using NotifyService.Domain.Entities;

namespace NotifyService.Application.Interfaces;
public interface IBatchProcessor
{
    Task AddToBatchAsync(NotificationMessage notification);
    Task FlushBatchAsync();
}