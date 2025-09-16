public interface IRedisStreamService
{
    Task AddMessageAsync<T>(string streamName, T message, string? messageId = null);

    Task<IReadOnlyList<RedisStreamMessage<T>>> ReadMessagesAsync<T>(
        string streamName, 
        string consumerGroup, 
        string consumerName, 
        int count = 10, 
        TimeSpan? block = null
    );

    Task AcknowledgeMessageAsync(string streamName, string consumerGroup, string messageId);

    Task CreateConsumerGroupAsync(string streamName, string consumerGroup);

    Task DeleteConsumerGroupAsync(string streamName, string consumerGroup);
}


public class RedisStreamMessage<T>
{
    public string Id { get; set; }
    public T Body { get; set; }
}