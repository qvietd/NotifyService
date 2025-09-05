using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotifyService.Domain.Entities;

public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("recipientId")]
    public string? RecipientId { get; set; }

    [BsonElement("senderId")]
    public string? SenderId { get; set; }

    [BsonElement("senderName")]
    public string? SenderName { get; set; }

    [BsonElement("senderAvatar")]
    public string? SenderAvatar { get; set; }

    [BsonElement("type")]
    public string? Type { get; set; } // event type

    [BsonElement("content")]
    public string? Content { get; set; }

    [BsonElement("link")]
    public string? Link { get; set; }

    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}