using StackExchange.Redis;

namespace NotifyService.Application.Interfaces;

public class ConnectionMappingService : IConnectionMappingService
{
    private readonly IDatabase _database;
    private const string USER_CONNECTIONS_PREFIX = "user_connections:";
    private const string CONNECTION_USER_PREFIX = "connection_user:";

    public ConnectionMappingService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task AddConnectionAsync(string userId, string connectionId)
    {
        await _database.SetAddAsync($"{USER_CONNECTIONS_PREFIX}{userId}", connectionId);
        await _database.StringSetAsync($"{CONNECTION_USER_PREFIX}{connectionId}", userId);
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        var userId = await _database.StringGetAsync($"{CONNECTION_USER_PREFIX}{connectionId}");
        if (userId.HasValue)
        {
            await _database.SetRemoveAsync($"{USER_CONNECTIONS_PREFIX}{userId}", connectionId);
            await _database.KeyDeleteAsync($"{CONNECTION_USER_PREFIX}{connectionId}");
        }
    }

    public async Task<List<string>> GetConnectionsAsync(string userId)
    {
        var connections = await _database.SetMembersAsync($"{USER_CONNECTIONS_PREFIX}{userId}");
        return connections.Select(c => c.ToString()).ToList();
    }
}