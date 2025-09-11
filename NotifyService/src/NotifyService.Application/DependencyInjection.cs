using NotifyService.Application.Interfaces;
using NotifyService.Application.Services;

namespace NotifyService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        //services.AddSingleton<IRabbitMqService, RabbitMQService>();
        //services.AddSingleton<IConnectionMappingService, ConnectionMappingService>();
        //services.AddSingleton<IBatchProcessor, BatchProcessor>();

        return services;
    }
}