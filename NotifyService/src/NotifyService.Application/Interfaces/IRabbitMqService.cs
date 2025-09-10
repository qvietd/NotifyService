using NotifyService.Application.Dtos;
using NotifyService.Domain.Entities;

namespace NotifyService.Application.Interfaces;

public interface IRabbitMqService
{
    // Task PublishAsync<T>(string queueName, T message);
    // Task StartConsumingAsync<T>(string queueName, Func<T, Task> processMesssage, int prefetchCount = 1);
    void PublishMessage(NotificationDto notification);
    void PublishToDeadLetter(NotificationMessage notification);
    void StartConsuming(Func<NotificationDto, Task<bool>> messageHandler);
    void StopConsuming();
}

