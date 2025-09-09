using MongoDB.Driver;
using NotifyService.Infrastructure.BackgroundServices;
using NotifyService.Infrastructure.Repositories;
using NotifyService.Infrastructure.Services;

namespace NotifyService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // MongoDB
        services.AddSingleton<IMongoClient>(sp =>
        {
            var connectionString = configuration.GetConnectionString("MongoDb");
            return new MongoClient(connectionString);
        });

        services.AddScoped(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var databaseName = configuration.GetValue<string>("MongoDb:DatabaseName");
            return client.GetDatabase(databaseName);
        });

        // Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserConnectionService, UserConnectionService>();
        services.AddScoped<IBatchProcessor, BatchProcessor>();

        // Background Services
        services.AddHostedService<RabbitMqConsumerService>();
        return services;
    }
}