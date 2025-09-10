using NotifyService.Application.Dtos;
using NotifyService.Application.Interfaces;
using NotifyService.Domain.Entities;
using NotifyService.Infrastructure.Repositories;

namespace NotifyService.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task<NotificationMessage> CreateOrUpdateNotificationAsync(NotificationRequestDto request)
    {
        try
        {
            // Generate batch key if not provided
            var batchKey = !string.IsNullOrEmpty(request.BatchKey) 
                ? request.BatchKey 
                : GenerateBatchKey(request);

            // Try to find existing notification with same batch key
            var existingNotification = await _notificationRepository.GetByBatchKeyAsync(request.UserId, batchKey);

            if (existingNotification != null)
            {
                // Update existing notification
                existingNotification.Count++;
                existingNotification.LastMessageContent = request.Message;
                existingNotification.UpdatedAt = DateTime.UtcNow;
                existingNotification.IsRead = false; // Mark as unread when updated
                existingNotification.SentViaSignalR = false; // Reset signalR flag
                existingNotification.SentViaEmail = false; // Reset email flag

                // Add to message history
                // existingNotification.MessageHistory.Add(new MessageHistoryItem
                // {
                //     Message = request.Message,
                //     ReceivedAt = DateTime.UtcNow,
                //     Metadata = request.Metadata
                // });

                // Update title with count if more than 1
                if (existingNotification.Count > 1)
                {
                    existingNotification.Title = $"{request.Title} ({existingNotification.Count} messages)";
                    existingNotification.Message = $"Latest: {request.Message}";
                }

                await _notificationRepository.UpdateAsync(existingNotification);
                _logger.LogInformation("Updated existing notification {NotificationId} with batch key {BatchKey}. New count: {Count}", 
                    existingNotification.Id, batchKey, existingNotification.Count);

                return existingNotification;
            }
            else
            {
                // Create new notification
                var notification = new NotificationMessage
                {
                    UserId = request.UserId,
                    UserEmail = request.UserEmail,
                    Title = request.Title,
                    Message = request.Message,
                    Type = request.Type,
                    BatchKey = batchKey,
                    LastMessageContent = request.Message,
                    //Metadata = request.Metadata,
                    // MessageHistory = new List<MessageHistoryItem>
                    // {
                    //     new MessageHistoryItem
                    //     {
                    //         Message = request.Message,
                    //         ReceivedAt = DateTime.UtcNow,
                    //         Metadata = request.Metadata
                    //     }
                    // }
                };

                var result = await _notificationRepository.CreateAsync(notification);
                _logger.LogInformation("Created new notification {NotificationId} with batch key {BatchKey}", 
                    result.Id, batchKey);

                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or updating notification for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<IEnumerable<NotificationMessage>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await _notificationRepository.GetUserNotificationsAsync(userId, page, pageSize);
    }

    public async Task<bool> MarkAsReadAsync(string notificationId)
    {
        return await _notificationRepository.MarkAsReadAsync(notificationId);
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId);
    }

    public async Task BatchProcessNotificationsAsync(List<NotificationRequestDto> notifications)
    {
        try
        {
            // Group notifications by user and batch key
            var groupedNotifications = notifications
                .GroupBy(n => new { n.UserId, BatchKey = !string.IsNullOrEmpty(n.BatchKey) ? n.BatchKey : GenerateBatchKey(n) })
                .ToList();

            foreach (var group in groupedNotifications)
            {
                var firstNotification = group.First();
                var batchKey = group.Key.BatchKey;

                // Check if notification exists
                var existingNotification = await _notificationRepository.GetByBatchKeyAsync(group.Key.UserId, batchKey);

                if (existingNotification != null)
                {
                    // Update existing with batch data
                    existingNotification.Count += group.Count();
                    existingNotification.LastMessageContent = group.Last().Message;
                    existingNotification.UpdatedAt = DateTime.UtcNow;
                    existingNotification.IsRead = false;
                    existingNotification.SentViaSignalR = false;
                    existingNotification.SentViaEmail = false;

                    // Add all messages to history
                    // foreach (var notification in group)
                    // {
                    //     existingNotification.MessageHistory.Add(new MessageHistoryItem
                    //     {
                    //         Message = notification.Message,
                    //         ReceivedAt = DateTime.UtcNow,
                    //         Metadata = notification.Metadata
                    //     });
                    // }

                    // Update title and message
                    existingNotification.Title = $"{firstNotification.Title} ({existingNotification.Count} messages)";
                    existingNotification.Message = $"Latest: {group.Last().Message}";

                    await _notificationRepository.UpdateAsync(existingNotification);
                    _logger.LogInformation("Batch updated notification {NotificationId} with {MessageCount} new messages", 
                        existingNotification.Id, group.Count());
                }
                else
                {
                    // Create new notification with batch data
                    var notification = new NotificationMessage
                    {
                        UserId = firstNotification.UserId,
                        UserEmail = firstNotification.UserEmail,
                        Title = group.Count() > 1 ? $"{firstNotification.Title} ({group.Count()} messages)" : firstNotification.Title,
                        Message = group.Count() > 1 ? $"Latest: {group.Last().Message}" : firstNotification.Message,
                        Type = firstNotification.Type,
                        BatchKey = batchKey,
                        Count = group.Count(),
                        LastMessageContent = group.Last().Message,
                        //Metadata = firstNotification.Metadata,
                        // MessageHistory = group.Select(n => new MessageHistoryItem
                        // {
                        //     Message = n.Message,
                        //     ReceivedAt = DateTime.UtcNow,
                        //     Metadata = n.Metadata
                        // }).ToList()
                    };

                    await _notificationRepository.CreateAsync(notification);
                    _logger.LogInformation("Batch created notification with {MessageCount} messages", group.Count());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch processing notifications");
            throw;
        }
    }
}