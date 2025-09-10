namespace NotifyService.Infrastructure.Configuration;

public class RabbitMQSettings
{
    public string ConnectionString { get; set; }
    public string QueueName { get; set; } = "notifications";
    public string DeadLetterQueueName { get; set; } = "notifications-dlq";
    public string ExchangeName { get; set; } = "notifications-exchange";
    public int PrefetchCount { get; set; } = 10;
    public int MaxRetryAttempts { get; set; } = 3;
}
