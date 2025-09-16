using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotifyService.Domain.Entities;

public class NotificationMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("senderId")]
    public string SenderId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("isRead")]
    public bool? IsRead { get; set; }

    [BsonElement("isSeen")]
    public bool? IsSeen { get; set; }

    [BsonElement("status")]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    [BsonElement("metadata")]
    public Dictionary<string, object?>? Metadata { get; set; }

    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("processedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ProcessedAt { get; set; }

    [BsonElement("readAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? ReadAt { get; set; }

    [BsonElement("retryCount")]
    public int RetryCount { get; set; } = 0;

    [BsonElement("error")]
    public string? Error { get; set; }

    [BsonIgnore]
    public ulong DeliveryTag { get; set; }
}

public enum NotificationStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    DeadLetter
}