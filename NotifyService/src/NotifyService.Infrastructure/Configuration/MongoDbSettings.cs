namespace NotifyService.Infrastructure.Configuration;
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "NotifyServiceDb";
    public string NotificationsCollection { get; set; } = "Notifications";
}