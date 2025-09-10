namespace NotifyService.Infrastructure.Configuration;

public class MongoDBSettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
    public string CollectionName { get; set; } = "notifications";
    public int BatchSize { get; set; } = 100;
    public int BatchTimeoutMs { get; set; } = 5000;
}