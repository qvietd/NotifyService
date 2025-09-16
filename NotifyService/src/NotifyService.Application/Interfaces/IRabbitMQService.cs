namespace NotifyService.NotifyService.Application.Interfaces;

public interface IRabbitMQService
{
    Task PublishAsync<T>(T message, string routingKey = "") where T : class;
    Task PublishBatchAsync<T>(IEnumerable<T> messages, string routingKey = "") where T : class;
    Task StartConsumingAsync(Func<string, Task<bool>> messageHandler);
    Task PublishToDeadLetterAsync(string message, string error);
    Task AcknowledgeMessageAsync(ulong deliveryTag);
    Task RejectMessageAsync(ulong deliveryTag, bool requeue);
}
