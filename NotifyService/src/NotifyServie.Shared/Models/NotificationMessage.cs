using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotifyService.Shared.Models;

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

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("isRead")]
    public bool IsRead { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("sentViaSignalR")]
    public bool SentViaSignalR { get; set; } = false;

    [BsonElement("sentViaEmail")]
    public bool SentViaEmail { get; set; } = false;

    [BsonElement("count")]
    public int Count { get; set; } = 1;

    [BsonElement("lastMessageContent")]
    public string LastMessageContent { get; set; } = string.Empty;

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Batch processing fields
    [BsonElement("batchKey")]
    public string BatchKey { get; set; } = string.Empty; // Unique key for grouping similar notifications

    //[BsonElement("messageHistory")]
    //public List<MessageHistoryItem> MessageHistory { get; set; } = new();
}

public class MessageHistoryItem
{
    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("receivedAt")]
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}