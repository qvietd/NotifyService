namespace NotifyService.Infrastructure.Configuration;
public class RabbitMqSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string NotificationQueue { get; set; } = "notifications";
    public string EmailQueue { get; set; } = "email.sending";
    public int PrefetchCount { get; set; } = 10;
}