using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotifyService.Domain.Entities;

public class NotificationMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("userEmail")]
    public string UserEmail { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The main content of the notification message
    /// </summary>
    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The event type of the notification (e.g., "comment", "like", "follow")
    /// </summary>
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of reactions (like, love, etc.) associated with this notification
    /// </summary>
    [BsonElement("count")]
    public int Count { get; set; } = 1;

    [BsonElement("reactByUsers")]
    public List<ReactByUser> ReactByUsers { get; set; } = new();

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
}

public class ReactByUser
{
    /// <summary>
    /// The reaction type (e.g., "like", "love", "haha")
    /// </summary>
    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("senderId")]
    public string SenderId { get; set; } = string.Empty;

    [BsonElement("senderEmail")]
    public string SenderEmail { get; set; } = string.Empty;

    [BsonElement("receivedAt")]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationStatus
{
    Pending,
    Processing,
    Sent,
    Failed,
    DeadLetter
}