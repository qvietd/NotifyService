using NotifyService.Application.Interfaces;
using NotifyService.Application.Services;

namespace NotifyService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationProcessor, NotificationProcessor>();
        
        return services;
    }
}