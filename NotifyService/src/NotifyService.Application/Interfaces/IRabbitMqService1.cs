public interface IRabbitMqService1
{
    Task ConnectAsync();
    void Disconnect();
    bool IsConnected { get; }

    Task PublishAsync<T>(string exchange, string routingKey, T message);

    void Subscribe<T>(string queue, Func<T, Task<bool>> messageHandler); // Return true = ack, false = nack

    void DeclareExchange(string exchangeName, string type = "direct", bool durable = true);
    void DeclareQueue(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false);
    void BindQueue(string queueName, string exchangeName, string routingKey);

    void ConfigureRetryPolicy(int maxRetries, TimeSpan delayBetweenRetries);
    void EnableDeadLetterQueue(string deadLetterExchange, string deadLetterRoutingKey);
}
