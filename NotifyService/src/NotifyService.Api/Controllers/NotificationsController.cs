using Microsoft.AspNetCore.Mvc;
using NotifyService.Application.Dtos;
using NotifyService.Application.Interfaces;

namespace NotifyService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IRabbitMqService _rabbitMQService;

    public NotificationsController(IRabbitMqService rabbitMQService)
    {
        _rabbitMQService = rabbitMQService;
    }

    [HttpPost("send")]
    public IActionResult SendNotification([FromBody] NotificationDto notification)
    {
        _rabbitMQService.PublishMessage(notification);
        return Ok(new { Message = "Notification queued successfully" });
    }
}
