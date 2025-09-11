using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotifyService.Domain.Interfaces;
using NotifyService.Infrastructure.Configuration;
using NotifyService.Infrastructure.Repositories;
using NotifyService.Infrastructure.Services;
using NotifyService.Infrastructure.Workers;
using StackExchange.Redis;

namespace NotifyService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        // Configure options
        services.Configure<RabbitMQConfig>(configuration.GetSection("RabbitMQ"));
        services.Configure<MongoDBConfig>(configuration.GetSection("MongoDB"));
        services.Configure<RedisConfig>(configuration.GetSection("Redis"));
        // MongoDB
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDBConfig>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var settings = sp.GetRequiredService<IOptions<MongoDBConfig>>().Value;
            return client.GetDatabase(settings.DatabaseName);
        });

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RedisConfig>>().Value;
            return ConnectionMultiplexer.Connect(settings.ConnectionString);
        });
        // Register services
        services.AddSingleton<IRabbitMQService, RabbitMQService>();
        services.AddSingleton<INotificationRepository, NotificationRepository>();

        // Add hosted services
        services.AddHostedService<MessageConsumerWorker>();
        services.AddHostedService<NotifySenderWorker>();
        return services;
    }
}