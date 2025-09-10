using MongoDB.Bson;
using NotifyService.Application.Dtos;
using NotifyService.Application.Interfaces;
using NotifyService.Domain.Entities;
using NotifyService.Infrastructure.Repositories;

namespace   NotifyService.Infrastructure.BackgroundServices;
public class MessageConsumerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageConsumerWorker> _logger;

    public MessageConsumerWorker(
        IServiceProvider serviceProvider,
        ILogger<MessageConsumerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var _rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMqService>();
        _rabbitMQService.StartConsuming(ProcessMessage);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _rabbitMQService.StopConsuming();
    }

    private async Task<bool> ProcessMessage(NotificationDto dto)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var batchProcessor = scope.ServiceProvider.GetRequiredService<IBatchProcessor>();

        try
        {
            var notification = new NotificationMessage
            {
                UserId = dto.UserId,
                ConnectionId = dto.ConnectionId,
                Title = dto.Title,
                Content = dto.Content,
                Type = dto.Type,
                Status = NotificationStatus.Pending
            };

            await batchProcessor.AddToBatchAsync(notification);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification message");
            return false;
        }
    }
}