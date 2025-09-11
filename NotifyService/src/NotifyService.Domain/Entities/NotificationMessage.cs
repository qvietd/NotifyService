using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotifyService.Domain.Entities;

public class NotificationMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public string MessageId { get; set; }
    public string UserId { get; set; }
    public string ConnectionId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public string ErrorMessage { get; set; }

    public DateTime? NextRetryAt { get; set; }
}

public enum NotificationStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    DeadLetter
}