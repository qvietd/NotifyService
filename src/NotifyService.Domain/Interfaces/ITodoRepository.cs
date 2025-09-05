using NotifyService.Domain.Entities;
using NotifyService.Domain.ValueObjects;

namespace NotifyService.Domain.Interfaces;

public interface ITodoRepository
{
    Task<Todo?> GetByIdAsync(TodoId id, CancellationToken cancellationToken = default);
    Task<List<Todo>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Todo>> GetByPriorityAsync(Priority priority, CancellationToken cancellationToken = default);
    Task<List<Todo>> GetCompletedAsync(CancellationToken cancellationToken = default);
    Task<List<Todo>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<Todo> AddAsync(Todo todo, CancellationToken cancellationToken = default);
    Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default);
    Task DeleteAsync(TodoId id, CancellationToken cancellationToken = default);
}