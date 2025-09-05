using NotifyService.Domain.ValueObjects;

namespace NotifyService.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendTodoCreatedNotificationAsync(Guid todoId, string title, CancellationToken cancellationToken = default);
    Task SendTodoCompletedNotificationAsync(Guid todoId, string title, DateTime completedAt, CancellationToken cancellationToken = default);
    Task SendTodoUpdatedNotificationAsync(Guid todoId, string title, string changeDescription, CancellationToken cancellationToken = default);
    Task SendTodoReopenedNotificationAsync(Guid todoId, string title, CancellationToken cancellationToken = default);
    Task SendTodoPriorityChangedNotificationAsync(Guid todoId, string title, Priority newPriority, CancellationToken cancellationToken = default);
}

public class TodoNotification
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class TodoCreatedNotification
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Priority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TodoCompletedNotification
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}

public class TodoUpdatedNotification
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
}