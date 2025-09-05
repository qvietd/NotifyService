using NotifyService.Domain.ValueObjects;

namespace NotifyService.Domain.Events;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
}

public record TodoCreatedEvent(
    Guid TodoId,
    string Title,
    string? Description,
    Priority Priority) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record TodoUpdatedEvent(
    Guid TodoId,
    string? OldValue,
    string? NewValue) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record TodoCompletedEvent(
    Guid TodoId,
    string Title,
    DateTime CompletedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record TodoReopenedEvent(
    Guid TodoId,
    string Title) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record TodoPriorityChangedEvent(
    Guid TodoId,
    Priority NewPriority) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}