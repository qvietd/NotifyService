namespace NotifyService.NotifyService.Application.Interfaces;

public interface IRabbitMQService
{
    Task PublishAsync<T>(T message, string routingKey = "", CancellationToken cancellationToken = default) where T : class;
    Task PublishBatchAsync<T>(IEnumerable<T> messages, string routingKey = "", CancellationToken cancellationToken = default) where T : class;
    Task StartConsumingAsync(Func<string, Task<bool>> messageHandler, CancellationToken cancellationToken = default);
    Task PublishToDeadLetterAsync(string message, string error, CancellationToken cancellationToken = default);
    Task AcknowledgeMessageAsync(ulong deliveryTag, CancellationToken cancellationToken = default);
    Task RejectMessageAsync(ulong deliveryTag, bool requeue, CancellationToken cancellationToken = default);
}
