using FluentValidation;
using MediatR;
using NotifyService.Application.Common.Interfaces;
using NotifyService.Domain.Interfaces;
using NotifyService.Domain.ValueObjects;

namespace NotifyService.Application.Features.NotifyService.Commands;

public record UpdateTodoCommand(
    Guid Id,
    string? Title,
    string? Description,
    Priority? Priority) : IRequest<UpdateTodoResponse>;

public record UpdateTodoResponse(
    Guid Id,
    string Title,
    string? Description,
    Priority Priority,
    bool IsCompleted,
    DateTime CreatedAt);

public class UpdateTodoCommandValidator : AbstractValidator<UpdateTodoCommand>
{
    public UpdateTodoCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required");

        RuleFor(x => x.Title)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Title))
            .WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description != null)
            .WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .When(x => x.Priority.HasValue)
            .WithMessage("Priority must be a valid value");
    }
}

public class UpdateTodoCommandHandler : IRequestHandler<UpdateTodoCommand, UpdateTodoResponse>
{
    private readonly ITodoRepository _repository;
    private readonly IEventBus _eventBus;

    public UpdateTodoCommandHandler(ITodoRepository repository, IEventBus eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }

    public async Task<UpdateTodoResponse> Handle(UpdateTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = await _repository.GetByIdAsync(TodoId.From(request.Id), cancellationToken)
            ?? throw new KeyNotFoundException($"Todo with id {request.Id} not found");

        if (!string.IsNullOrEmpty(request.Title))
            todo.UpdateTitle(request.Title);

        if (request.Description != null)
            todo.UpdateDescription(request.Description);

        if (request.Priority.HasValue)
            todo.SetPriority(request.Priority.Value);

        await _repository.UpdateAsync(todo, cancellationToken);

        // Publish domain events
        foreach (var domainEvent in todo.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent, cancellationToken);
        }
        
        todo.ClearDomainEvents();

        return new UpdateTodoResponse(
            todo.Id.Value,
            todo.Title,
            todo.Description,
            todo.Priority,
            todo.IsCompleted,
            todo.CreatedAt);
    }
}