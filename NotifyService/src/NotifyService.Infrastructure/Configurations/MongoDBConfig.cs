namespace NotifyService.Infrastructure.Configuration;

public class MongoDBConfig
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
    public string NotificationsCollection { get; set; } = "notifications";
    public string OutboxCollection { get; set; } = "outbox_events";
}