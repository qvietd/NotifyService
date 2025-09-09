namespace NotifyService.Shared.Models;

public class NotificationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    //public Dictionary<string, object> Metadata { get; set; } = new();
    public string BatchKey { get; set; } = string.Empty; // For grouping similar notifications
}