using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotifyService.Application.Features.NotifyService.Commands;
using NotifyService.Application.Features.NotifyService.Queries;
using NotifyService.Domain.ValueObjects;

namespace NotifyService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TodosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TodosController> _logger;

    public TodosController(IMediator mediator, ILogger<TodosController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all todos
    /// </summary>
    /// <returns>List of todos</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<GetTodosResponse>> GetTodos(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTodosQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get todo by id
    /// </summary>
    /// <param name="id">Todo identifier</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Todo details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoDto>> GetTodo(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTodoByIdQuery(id), cancellationToken);
        
        if (result == null)
        {
            return NotFound($"Todo with id {id} not found");
        }

        return Ok(result);
    }

    /// <summary>
    /// Get todos by priority
    /// </summary>
    /// <param name="priority">Priority level</param>
    /// <param name="cancellationToken"></param>
    /// <returns>List of todos with specified priority</returns>
    [HttpGet("priority/{priority}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetTodosResponse>> GetTodosByPriority(Priority priority, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(Priority), priority))
        {
            return BadRequest("Invalid priority value");
        }

        var result = await _mediator.Send(new GetTodosByPriorityQuery(priority), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new todo
    /// </summary>
    /// <param name="request">Todo creation request</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Created todo</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateTodoResponse>> CreateTodo([FromBody] CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTodoCommand(request.Title, request.Description, request.Priority);
        
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetTodo), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing todo
    /// </summary>
    /// <param name="id">Todo identifier</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Updated todo</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateTodoResponse>> UpdateTodo(Guid id, [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new UpdateTodoCommand(id, request.Title, request.Description, request.Priority);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Mark todo as completed
    /// </summary>
    /// <param name="id">Todo identifier</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Updated todo</returns>
    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompleteTodoResponse>> CompleteTodo(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CompleteTodoCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Reopen a completed todo
    /// </summary>
    /// <param name="id">Todo identifier</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Updated todo</returns>
    [HttpPatch("{id:guid}/reopen")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReopenTodoResponse>> ReopenTodo(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new ReopenTodoCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Delete a todo
    /// </summary>
    /// <param name="id">Todo identifier</param>
    /// <param name="cancellationToken"></param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTodo(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteTodoCommand(id);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}

// DTOs for API requests
public record CreateTodoRequest(
    string Title,
    string? Description = null,
    Priority Priority = Priority.Medium);

public record UpdateTodoRequest(
    string? Title = null,
    string? Description = null,
    Priority? Priority = null);