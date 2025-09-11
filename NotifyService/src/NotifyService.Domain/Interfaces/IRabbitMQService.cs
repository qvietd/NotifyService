namespace NotifyService.Domain.Interfaces;

public interface IRabbitMQService
{
    void StartConsuming(Func<string, Task<bool>> messageHandler);
    void PublishToDeadLetter(string message, string error);
    void AcknowledgeMessage(ulong deliveryTag);
    void RejectMessage(ulong deliveryTag, bool requeue);
    void Dispose();
}
