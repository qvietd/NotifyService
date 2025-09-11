namespace NotifyService.Infrastructure.Configuration;

public class MongoDBConfig
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
    public string CollectionName { get; set; }
    public int BatchSize { get; set; } = 100;
    public int BatchTimeoutMs { get; set; } = 5000;
}