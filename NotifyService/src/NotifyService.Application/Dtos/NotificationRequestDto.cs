namespace NotifyService.Application.Dtos;

public class NotificationRequestDto
{
    public string senderId { get; set; } = string.Empty;
    public string senderEmail { get; set; } = string.Empty;
    public string eventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}