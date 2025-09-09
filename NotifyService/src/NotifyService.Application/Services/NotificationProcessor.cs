using Microsoft.AspNetCore.SignalR;
using NotifyService.Application.Interfaces;
using NotifyService.Infrastructure.Hubs;
using NotifyService.Infrastructure.Services;
using NotifyService.Shared.Models;

namespace NotifyService.Application.Services;

public class NotificationProcessor : INotificationProcessor
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IEmailService _emailService;
    private readonly IUserConnectionService _userConnectionService;
    private readonly ILogger<NotificationProcessor> _logger;

    public NotificationProcessor(
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext,
        IEmailService emailService,
        IUserConnectionService userConnectionService,
        ILogger<NotificationProcessor> logger)
    {
        _notificationService = notificationService;
        _hubContext = hubContext;
        _emailService = emailService;
        _userConnectionService = userConnectionService;
        _logger = logger;
    }

    public async Task ProcessNotificationAsync(NotificationRequest request)
    {
        try
        {
            // 1. Create or update notification in database
            var notification = await _notificationService.CreateOrUpdateNotificationAsync(request);
            _logger.LogInformation("Processed notification {NotificationId} for user {UserId}", 
                notification.Id, request.UserId);

            // 2. Send via SignalR if user is online
            if (_userConnectionService.IsUserOnline(request.UserId))
            {
                await SendSignalRNotificationAsync(request.UserId, notification);
            }

            // 3. Send email notification
            await _emailService.SendNotificationEmailAsync(
                request.UserEmail,
                notification.Title,
                notification.Message,
                request.Type);

            _logger.LogInformation("Completed processing notification for user {UserId}", request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task ProcessBatchNotificationsAsync(List<NotificationRequest> requests)
    {
        try
        {
            // 1. Batch process notifications in database
            await _notificationService.BatchProcessNotificationsAsync(requests);
            _logger.LogInformation("Batch processed {Count} notifications", requests.Count);

            // 2. Group by user for SignalR and email notifications
            var userGroups = requests.GroupBy(r => r.UserId);

            foreach (var userGroup in userGroups)
            {
                var userId = userGroup.Key;
                var userNotifications = userGroup.ToList();

                // Get the latest updated notifications for this user
                var latestNotifications = await _notificationService.GetUserNotificationsAsync(userId, 1, userNotifications.Count);

                // Send SignalR if user is online
                if (_userConnectionService.IsUserOnline(userId))
                {
                    foreach (var notification in latestNotifications)
                    {
                        await SendSignalRNotificationAsync(userId, notification);
                    }
                }

                // Send consolidated email
                var firstRequest = userNotifications.First();
                var totalCount = userNotifications.Count;
                
                var emailTitle = totalCount > 1 
                    ? $"You have {totalCount} new notifications"
                    : firstRequest.Title;
                
                var emailMessage = totalCount > 1
                    ? $"You have received {totalCount} new notifications. Latest: {userNotifications.Last().Message}"
                    : firstRequest.Message;

                await _emailService.SendNotificationEmailAsync(
                    firstRequest.UserEmail,
                    emailTitle,
                    emailMessage,
                    firstRequest.Type);
            }

            _logger.LogInformation("Completed batch processing for {UserCount} users", userGroups.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch processing notifications");
            throw;
        }
    }

    private async Task SendSignalRNotificationAsync(string userId, NotificationMessage notification)
    {
        try
        {
            var connectionIds = _userConnectionService.GetUserConnections(userId);
            foreach (var connectionId in connectionIds)
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type,
                    count = notification.Count,
                    createdAt = notification.CreatedAt,
                    updatedAt = notification.UpdatedAt,
                    metadata = notification.Metadata
                });
            }

            _logger.LogInformation("Sent SignalR notification to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SignalR notification to user {UserId}", userId);
        }
    }
}
