
using NotifyService.Application.Dtos;

namespace NotifyService.Application.Interfaces;
public interface INotificationProcessor
{
    Task ProcessNotificationAsync(NotificationRequestDto request);
    Task ProcessBatchNotificationsAsync(List<NotificationRequestDto> requests);
}