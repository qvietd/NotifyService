using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NotifyService.Api.Hubs;
using NotifyService.Domain.Entities;
using NotifyService.Infrastructure.Repositories;

namespace NotifyService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotifyController : ControllerBase
{
    private readonly INotificationRepository _repository;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotifyController> _logger;

    public NotifyController(
        INotificationRepository repository,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotifyController> logger)
    {
        _repository = repository;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification([FromBody] NotificationMessage message)
    {
        try
        {
            message.MessageId = Guid.NewGuid().ToString();
            message.CreatedAt = DateTime.UtcNow;
            message.Status = NotificationStatus.Pending;

            await _repository.BatchInsertAsync(new[] { message });

            return Ok(new { success = true, messageId = message.MessageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
            return StatusCode(500, new { error = "Failed to send notification" });
        }
    }

    [HttpGet("status/{messageId}")]
    public async Task<IActionResult> GetMessageStatus(string messageId)
    {
        var message = await _repository.GetMessageByIdAsync(messageId);
        if (message == null)
            return NotFound();

        return Ok(message);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingMessages([FromQuery] int limit = 100)
    {
        var messages = await _repository.GetPendingMessagesAsync(limit);
        return Ok(messages);
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> BroadcastMessage([FromBody] object content)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", content);
        return Ok(new { success = true });
    }
}