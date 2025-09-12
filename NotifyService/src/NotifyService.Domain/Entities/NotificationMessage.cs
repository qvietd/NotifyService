using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotifyService.Domain.Entities;

public class NotificationMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }
    public required string MessageId { get; set; }
    public string UserId { get; set; }
    public string UserEmail { get; set; }
    public string UserSenderId { get; set; }
    public string UserSenderEmail { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    // all type of event like: message, friend_request, system_alert, etc.
    public string Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public DateTime? NextRetryAt { get; set; }
    public string? ErrorMessage { get; set; }
    // add more metadata if needed
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum NotificationStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    DeadLetter
}