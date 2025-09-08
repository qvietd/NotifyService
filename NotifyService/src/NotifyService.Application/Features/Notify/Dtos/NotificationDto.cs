using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotifyService.Application.Features.Notify.Queries;

public class NotificationDto
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("recipientId")]
    public string? RecipientId { get; set; }

    [BsonElement("senderId")]
    public string? SenderId { get; set; }

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

    public List<ActorInfo> Actors { get; set; } = new();
}