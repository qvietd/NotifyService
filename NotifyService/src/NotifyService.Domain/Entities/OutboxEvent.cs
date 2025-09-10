using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotifyService.Domain.Entities;

public class OutboxEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]

    public string Id { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; } = false;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; } = 0;
}