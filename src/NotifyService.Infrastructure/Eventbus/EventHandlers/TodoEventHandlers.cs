using NotifyService.Application.Common.Interfaces;
using NotifyService.Domain.Events;

namespace NotifyService.Infrastructure.EventBus.EventHandlers;

public class TodoCreatedEventHandler : IDomainEventHandler<TodoCreatedEvent>
{
    private readonly ILogger<TodoCreatedEventHandler> _logger;

    public TodoCreatedEventHandler(ILogger<TodoCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(TodoCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Todo created: {TodoId} - {Title}", domainEvent.TodoId, domainEvent.Title);
        
        // Additional business logic can be added here
        // e.g., Send welcome email, Update analytics, etc.
        
        await Task.CompletedTask;
    }
}

public class TodoCompletedEventHandler : IDomainEventHandler<TodoCompletedEvent>
{
    private readonly ILogger<TodoCompletedEventHandler> _logger;

    public TodoCompletedEventHandler(ILogger<TodoCompletedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(TodoCompletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Todo completed: {TodoId} - {Title} at {CompletedAt}", 
            domainEvent.TodoId, domainEvent.Title, domainEvent.CompletedAt);
        
        // Additional business logic can be added here
        // e.g., Send congratulations notification, Update statistics, etc.
        
        await Task.CompletedTask;
    }
}

public class TodoUpdatedEventHandler : IDomainEventHandler<TodoUpdatedEvent>
{
    private readonly ILogger<TodoUpdatedEventHandler> _logger;

    public TodoUpdatedEventHandler(ILogger<TodoUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(TodoUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Todo updated: {TodoId} - Changed from '{OldValue}' to '{NewValue}'", 
            domainEvent.TodoId, domainEvent.OldValue, domainEvent.NewValue);
        
        await Task.CompletedTask;
    }
}