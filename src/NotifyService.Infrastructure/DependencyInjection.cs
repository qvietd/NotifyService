using Microsoft.EntityFrameworkCore;
using NotifyService.Application.Common.Interfaces;
using NotifyService.Domain.Events;
using NotifyService.Domain.Interfaces;
using NotifyService.Infrastructure.Data;
using NotifyService.Infrastructure.EventBus;
using NotifyService.Infrastructure.EventBus.EventHandlers;
using NotifyService.Infrastructure.Notifications;
using NotifyService.Infrastructure.Repositories;

namespace NotifyService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<TodoDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                // Use In-Memory database for development/testing
                options.UseInMemoryDatabase("TodoDb");
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        // Repositories
        services.AddScoped<ITodoRepository, TodoRepository>();

        // Event Bus - Use Enhanced version with retry logic
        services.AddSingleton<IEventBus, RabbitMQEventBusService>();
        // Notification Service
        services.AddScoped<INotificationService, SignalRNotificationService>();
        
        // Event Handlers
        services.AddTransient<IDomainEventHandler<TodoCreatedEvent>, TodoCreatedEventHandler>();
        services.AddTransient<IDomainEventHandler<TodoCompletedEvent>, TodoCompletedEventHandler>();
        services.AddTransient<IDomainEventHandler<TodoUpdatedEvent>, TodoUpdatedEventHandler>();

        // Background Services
        services.AddHostedService<RabbitMQConsumerService>();
        return services;
    }
}