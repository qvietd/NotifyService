using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotifyService.Infrastructure.BackgroundServices;
using NotifyService.Infrastructure.Configuration;
using NotifyService.Infrastructure.Repositories;
using StackExchange.Redis;

namespace NotifyService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        // Configuration
        services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
        services.Configure<MongoDBSettings>(configuration.GetSection("MongoDB"));
        services.Configure<RedisSettings>(configuration.GetSection("Redis"));
        // MongoDB
        // MongoDB
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
            return client.GetDatabase(settings.DatabaseName);
        });

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            return ConnectionMultiplexer.Connect(settings.ConnectionString);
        });

        // Services
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Background services
        services.AddHostedService<MessageConsumerWorker>();
        services.AddHostedService<NotificationSenderWorker>();
        return services;
    }
}