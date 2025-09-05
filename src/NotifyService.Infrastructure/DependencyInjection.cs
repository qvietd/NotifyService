namespace NotifyService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
       
        // Repositories

        // Event Bus - Use Enhanced version with retry logic
        // services.AddSingleton<IEventBus, RabbitMQEventBusService>();
        // Notification Service
        // services.AddScoped<INotificationService, SignalRNotificationService>();
        
        // Event Handlers

        // Background Services
        // services.AddHostedService<RabbitMQConsumerService>();
        return services;
    }
}