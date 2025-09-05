using FluentValidation;
using MediatR;
using NotifyService.Application.Common.Interfaces;
using NotifyService.Domain.Entities;
using NotifyService.Domain.Interfaces;
using NotifyService.Domain.ValueObjects;

namespace NotifyService.Application.Features.NotifyService.Commands;

public record CreateTodoCommand(
    string Title,
    string? Description,
    Priority Priority = Priority.Medium) : IRequest<CreateTodoResponse>;

public record CreateTodoResponse(
    Guid Id,
    string Title,
    string? Description,
    Priority Priority,
    bool IsCompleted,
    DateTime CreatedAt);

public class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Title is required and must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Priority must be a valid value");
    }
}

public class CreateTodoCommandHandler : IRequestHandler<CreateTodoCommand, CreateTodoResponse>
{
    private readonly ITodoRepository _repository;
    private readonly IEventBus _eventBus;

    public CreateTodoCommandHandler(ITodoRepository repository, IEventBus eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }

    public async Task<CreateTodoResponse> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        var todo = new Todo(request.Title, request.Description, request.Priority);
        
        await _repository.AddAsync(todo, cancellationToken);

        // Publish domain events
        foreach (var domainEvent in todo.DomainEvents)
        {
            await _eventBus.PublishAsync(domainEvent, cancellationToken);
        }
        
        todo.ClearDomainEvents();

        return new CreateTodoResponse(
            todo.Id.Value,
            todo.Title,
            todo.Description,
            todo.Priority,
            todo.IsCompleted,
            todo.CreatedAt);
    }
}