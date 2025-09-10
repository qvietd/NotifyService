namespace NotifyService.Infrastructure.Configuration;

public class RabbitMQConfig
{
    public string HostName { get; set; }
    public int Port { get; set; } = 5672;
    public string UserName { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; } = "/";
    public string QueueName { get; set; }
    public string DeadLetterQueue { get; set; }
    public int PrefetchCount { get; set; } = 10;
    public int MaxRetryCount { get; set; } = 3;
}