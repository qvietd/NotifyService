using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotifyService.Infrastructure.Configuration;
using NotifyService.Infrastructure.Repositories;
using NotifyService.Infrastructure.Services;
using NotifyService.Infrastructure.Workers;
using NotifyService.NotifyService.Application.Interfaces;
using NotifyService.src.NotifyService.Application.Interfaces;
using RabbitMQ.Client;
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

        services.AddSingleton(sp =>
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
        // RabbitMQ
        services.AddSingleton(sp =>
        {
            var _config = sp.GetRequiredService<IOptions<RabbitMQConfig>>().Value;
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
            return factory.CreateConnection();
        });
        services.AddSingleton<IRabbitMQService, RabbitMQService>();
        // regis ter services
        services.AddSingleton<INotificationRepository, NotificationRepository>();

        // Add hosted services
        services.AddHostedService<MessageConsumerWorker>();
        services.AddHostedService<NotifySenderWorker>();
        return services;
    }
}