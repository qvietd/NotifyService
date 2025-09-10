namespace NotifyService.Application.Interfaces;

public interface IRabbitMqService
{
    Task PublishAsync<T>(string queueName, T message);
    Task StartConsumingAsync<T>(string queueName, Func<T, Task> processMesssage, int prefetchCount = 1);
    void Dispose();
}

