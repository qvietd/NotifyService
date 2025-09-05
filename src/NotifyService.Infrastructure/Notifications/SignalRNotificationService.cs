using Microsoft.AspNetCore.SignalR;
using NotifyService.Api.Hubs;
using NotifyService.Application.Common.Interfaces;
using NotifyService.Domain.ValueObjects;

namespace NotifyService.Infrastructure.Notifications;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<TodoNotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<TodoNotificationHub> hubContext, 
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendTodoCreatedNotificationAsync(Guid todoId, string title, CancellationToken cancellationToken = default)
    {
        var notification = new TodoNotification
        {
            Type = "TodoCreated",
            Message = $"New todo created: {title}",
            Data = new TodoCreatedNotification
            {
                Id = todoId,
                Title = title,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, cancellationToken);
        _logger.LogInformation("Sent todo created notification for {TodoId}", todoId);
    }

    public async Task SendTodoCompletedNotificationAsync(Guid todoId, string title, DateTime completedAt, CancellationToken cancellationToken = default)
    {
        var notification = new TodoNotification
        {
            Type = "TodoCompleted",
            Message = $"Todo completed: {title}",
            Data = new TodoCompletedNotification
            {
                Id = todoId,
                Title = title,
                CompletedAt = completedAt
            }
        };

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, cancellationToken);
        _logger.LogInformation("Sent todo completed notification for {TodoId}", todoId);
    }

    public async Task SendTodoUpdatedNotificationAsync(Guid todoId, string title, string changeDescription, CancellationToken cancellationToken = default)
    {
        var notification = new TodoNotification
        {
            Type = "TodoUpdated",
            Message = $"Todo updated: {title}",
            Data = new TodoUpdatedNotification
            {
                Id = todoId,
                Title = title,
                ChangeDescription = changeDescription
            }
        };

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, cancellationToken);
        _logger.LogInformation("Sent todo updated notification for {TodoId}", todoId);
    }

    public async Task SendTodoReopenedNotificationAsync(Guid todoId, string title, CancellationToken cancellationToken = default)
    {
        var notification = new TodoNotification
        {
            Type = "TodoReopened",
            Message = $"Todo reopened: {title}",
            Data = new { Id = todoId, Title = title }
        };

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, cancellationToken);
        _logger.LogInformation("Sent todo reopened notification for {TodoId}", todoId);
    }

    public async Task SendTodoPriorityChangedNotificationAsync(Guid todoId, string title, Priority newPriority, CancellationToken cancellationToken = default)
    {
        var notification = new TodoNotification
        {
            Type = "TodoPriorityChanged",
            Message = $"Todo priority changed: {title} - {newPriority}",
            Data = new { Id = todoId, Title = title, NewPriority = newPriority }
        };

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, cancellationToken);
        _logger.LogInformation("Sent todo priority changed notification for {TodoId}", todoId);
    }
}