using Microsoft.AspNetCore.SignalR;
using NotifyService.Api.Hubs;
using NotifyService.Domain.Entities;
using NotifyService.Infrastructure.Repositories;

namespace NotifyService.Infrastructure.Workers;

public class NotifySenderWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotifySenderWorker> _logger;
    private readonly int _batchSize = 50;
    private readonly int _delayBetweenBatches = 1000;

    public NotifySenderWorker(
        IServiceProvider serviceProvider,
        ILogger<NotifySenderWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotifySenderWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

                // Get pending messages
                var pendingMessages = await repository.GetPendingMessagesAsync(_batchSize);

                if (pendingMessages.Any())
                {
                    await ProcessMessages(pendingMessages, repository, hubContext);
                }

                // Process retry messages
                var retryMessages = await repository.GetFailedMessagesForRetryAsync();
                if (retryMessages.Any())
                {
                    await ProcessRetryMessages(retryMessages, repository, hubContext);
                }

                await Task.Delay(_delayBetweenBatches, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotifySenderWorker");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessMessages(
        IEnumerable<NotificationMessage> messages,
        INotificationRepository repository,
        IHubContext<NotificationHub> hubContext)
    {
        var tasks = messages.Select(async message =>
        {
            try
            {
                // Update status to processing
                await repository.UpdateMessageStatusAsync(message.MessageId, NotificationStatus.Processing);

                // Send via SignalR
                if (!string.IsNullOrEmpty(message.MessageId))
                {
                    await hubContext.Clients.Client(message.MessageId)
                        .SendAsync("ReceiveNotification", message);
                }
                else if (!string.IsNullOrEmpty(message.UserId))
                {
                    await hubContext.Clients.User(message.UserId)
                        .SendAsync("ReceiveNotification", message);
                }
                else
                {
                    await hubContext.Clients.All
                        .SendAsync("ReceiveNotification", message);
                }

                // Update status to sent
                await repository.UpdateMessageStatusAsync(message.MessageId, NotificationStatus.Sent);
                _logger.LogInformation($"Message {message.MessageId} sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send message {message.MessageId}");
                await HandleFailedMessage(message, repository, ex.Message);
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task ProcessRetryMessages(
        IEnumerable<NotificationMessage> messages,
        INotificationRepository repository,
        IHubContext<NotificationHub> hubContext)
    {
        foreach (var message in messages)
        {
            try
            {
                _logger.LogInformation($"Retrying message {message.MessageId}, attempt {message.RetryCount + 1}");

                // Send via SignalR
                if (!string.IsNullOrEmpty(message.ConnectionId))
                {
                    await hubContext.Clients.Client(message.ConnectionId)
                        .SendAsync("ReceiveNotification", message);
                }
                else if (!string.IsNullOrEmpty(message.UserId))
                {
                    await hubContext.Clients.User(message.UserId)
                        .SendAsync("ReceiveNotification", message);
                }

                await repository.UpdateMessageStatusAsync(message.MessageId, NotificationStatus.Sent);
            }
            catch (Exception ex)
            {
                await HandleFailedMessage(message, repository, ex.Message);
            }
        }
    }

    private async Task HandleFailedMessage(NotificationMessage message, INotificationRepository repository, string error)
    {
        message.RetryCount++;

        if (message.RetryCount >= 5)
        {
            // Move to dead letter
            await repository.UpdateMessageStatusAsync(message.MessageId, NotificationStatus.DeadLetter, error);
            _logger.LogWarning($"Message {message.MessageId} moved to dead letter after {message.RetryCount} retries");
        }
        else
        {
            // Calculate next retry with exponential backoff
            var delay = CalculateExponentialBackoff(message.RetryCount);
            message.NextRetryAt = DateTime.UtcNow.Add(delay);

            await repository.UpdateMessageStatusAsync(message.MessageId, NotificationStatus.Failed, error);
            _logger.LogInformation($"Message {message.MessageId} will retry at {message.NextRetryAt}");
        }
    }

    private TimeSpan CalculateExponentialBackoff(int retryCount)
    {
        // Exponential backoff: 2^retryCount seconds with max of 5 minutes
        var seconds = Math.Min(Math.Pow(2, retryCount), 300);
        return TimeSpan.FromSeconds(seconds);
    }
}