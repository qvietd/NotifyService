namespace NotifyService.Infrastructure.Configuration;

public class RabbitMQConfig
{
    public string HostName { get; set; }
    public int Port { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; }
    public string NotifyQueue { get; set; }
    public string DeadLetterQueue { get; set; }
    public string Exchange { get; set; }
    public string DeadLetterExchange { get; set; }
    public int PrefetchCount { get; set; } = 10;
    public int MaxRetryCount { get; set; } = 5;
    public int InitialRetryDelayMs { get; set; } = 1000;
}
