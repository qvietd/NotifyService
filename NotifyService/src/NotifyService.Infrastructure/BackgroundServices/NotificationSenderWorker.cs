using Microsoft.AspNetCore.SignalR;
using NotifyService.Application.Interfaces;
using NotifyService.Domain.Entities;
using NotifyService.Infrastructure.Hubs;
using NotifyService.Infrastructure.Repositories;
using Polly;

namespace NotifyService.Infrastructure.BackgroundServices;

public class NotificationSenderWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IConnectionMappingService _connectionMapping;
    private readonly ILogger<NotificationSenderWorker> _logger;

    public NotificationSenderWorker(
        IServiceProvider serviceProvider,
        IHubContext<NotificationHub> hubContext,
        IConnectionMappingService connectionMapping,
        ILogger<NotificationSenderWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _connectionMapping = connectionMapping;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                // Get pending notifications (implement this method)
                var pendingNotifications = await repository.GetPendingNotificationsAsync();

                foreach (var notification in pendingNotifications)
                {
                    await ProcessNotificationWithRetry(notification, repository);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification sender worker");
            }

            await Task.Delay(5000, stoppingToken); // Check every 5 seconds
        }
    }

    private async Task ProcessNotificationWithRetry(NotificationMessage notification, INotificationRepository repository)
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: async (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for notification {NotificationId} in {Delay}ms",
                        retryCount, notification.Id, timespan.TotalMilliseconds);
                    await repository.IncrementRetryCountAsync(notification.Id);
                });

        try
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                await SendNotification(notification);
                await repository.UpdateStatusAsync(notification.Id, NotificationStatus.Sent);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId} after all retries", notification.Id);

            if (notification.RetryCount >= 3)
            {
                await repository.UpdateStatusAsync(notification.Id, NotificationStatus.DeadLetter, ex.Message);
                // Optionally send to DLQ for analysis
            }
            else
            {
                await repository.UpdateStatusAsync(notification.Id, NotificationStatus.Failed, ex.Message);
            }
        }
    }

    private async Task SendNotification(NotificationMessage notification)
    {
        if (!string.IsNullOrEmpty(notification.ConnectionId))
        {
            // Send to specific connection
            await _hubContext.Clients.Client(notification.ConnectionId)
                .SendAsync("ReceiveNotification", notification);
        }
        else if (!string.IsNullOrEmpty(notification.UserId))
        {
            // Send to all user connections
            await _hubContext.Clients.Group($"User_{notification.UserId}")
                .SendAsync("ReceiveNotification", notification);
        }
        else
        {
            throw new InvalidOperationException("Either ConnectionId or UserId must be specified");
        }
    }
}