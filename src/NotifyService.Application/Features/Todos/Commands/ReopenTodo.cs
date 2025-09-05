using MediatR;
using NotifyService.Application.Common.Interfaces;
using NotifyService.Domain.Interfaces;
using NotifyService.Domain.ValueObjects;

namespace NotifyService.Application.Features.NotifyService.Commands;

public record ReopenTodoCommand(Guid Id) : IRequest<ReopenTodoResponse>;

public record ReopenTodoResponse(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTime? CompletedAt);

public class ReopenTodoCommandHandler : IRequestHandler<ReopenTodoCommand, ReopenTodoResponse>
{
    private readonly ITodoRepository _repository;
    private readonly IEventBus _eventBus;

    public ReopenTodoCommandHandler(ITodoRepository repository, IEventBus eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }

    public async Task<ReopenTodoResponse> Handle(ReopenTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = await _repository.GetByIdAsync(TodoId.From(request.Id), cancellationToken)
            ?? throw new KeyNotFoundException($"Todo with id {request.Id} not found");

        todo.Reopen();
        await _repository.UpdateAsync(todo, cancellationToken);

        // Publish domain events
        foreach (var domainEvent in todo.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent, cancellationToken);
        }
        
        todo.ClearDomainEvents();

        return new ReopenTodoResponse(
            todo.Id.Value,
            todo.Title,
            todo.IsCompleted,
            todo.CompletedAt);
    }
}