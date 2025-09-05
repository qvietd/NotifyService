using Microsoft.EntityFrameworkCore;
using NotifyService.Domain.Entities;
using NotifyService.Domain.Interfaces;
using NotifyService.Domain.ValueObjects;
using NotifyService.Infrastructure.Data;

namespace NotifyService.Infrastructure.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly TodoDbContext _context;

    public TodoRepository(TodoDbContext context)
    {
        _context = context;
    }

    public async Task<Todo?> GetByIdAsync(TodoId id, CancellationToken cancellationToken = default)
    {
        return await _context.NotifyService
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<Todo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NotifyService
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Todo>> GetByPriorityAsync(Priority priority, CancellationToken cancellationToken = default)
    {
        return await _context.NotifyService
            .Where(t => t.Priority == priority)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Todo>> GetCompletedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NotifyService
            .Where(t => t.IsCompleted)
            .OrderByDescending(t => t.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Todo>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NotifyService
            .Where(t => !t.IsCompleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Todo> AddAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        _context.NotifyService.Add(todo);
        await _context.SaveChangesAsync(cancellationToken);
        return todo;
    }

    public async Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        _context.Entry(todo).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TodoId id, CancellationToken cancellationToken = default)
    {
        var todo = await GetByIdAsync(id, cancellationToken);
        if (todo != null)
        {
            _context.NotifyService.Remove(todo);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}