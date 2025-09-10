using Microsoft.AspNetCore.Mvc;
using NotifyService.Application.Dtos;
using NotifyService.Application.Interfaces;
using NotifyService.Shared.Models;

namespace NotifyService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly INotificationProcessor _notificationProcessor;

    public NotificationsController(
        INotificationService notificationService,
        INotificationProcessor notificationProcessor)
    {
        _notificationService = notificationService;
        _notificationProcessor = notificationProcessor;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserNotifications(
        string userId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20)
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize);
        return Ok(new
        {
            data = notifications,
            page,
            pageSize,
            total = notifications.Count()
        });
    }

    [HttpPost("{notificationId}/mark-read")]
    public async Task<IActionResult> MarkAsRead(string notificationId)
    {
        var result = await _notificationService.MarkAsReadAsync(notificationId);
        return result ? Ok() : NotFound();
    }

    [HttpGet("{userId}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(string userId)
    {
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationRequestDto request)
    {
        try
        {
            await _notificationProcessor.ProcessNotificationAsync(request);
            return Ok(new { message = "Notification sent successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("send-batch")]
    public async Task<IActionResult> SendBatchNotifications([FromBody] List<NotificationRequest> requests)
    {
        try
        {
            await _notificationProcessor.ProcessBatchNotificationsAsync(requests);
            return Ok(new { message = $"Batch of {requests.Count} notifications sent successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{userId}/history/{notificationId}")]
    public async Task<IActionResult> GetNotificationHistory(string userId, string notificationId)
    {
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, 1, 1000);
        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
        
        if (notification == null)
            return NotFound();

        return Ok(new
        {
            id = notification.Id,
            title = notification.Title,
            type = notification.Type,
            count = notification.Count,
            createdAt = notification.CreatedAt,
            updatedAt = notification.UpdatedAt,
            //messageHistory = notification.MessageHistory.OrderBy(h => h.ReceivedAt)
        });
    }
}
