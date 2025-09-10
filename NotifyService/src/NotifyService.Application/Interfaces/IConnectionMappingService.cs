namespace NotifyService.Application.Interfaces;

public interface IConnectionMappingService
{
    Task AddConnectionAsync(string userId, string connectionId);
    Task RemoveConnectionAsync(string connectionId);
    Task<List<string>> GetConnectionsAsync(string userId);
}