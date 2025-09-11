using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace NotifyService.Api.Hubs;

public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _redisDb;

    public NotificationHub(ILogger<NotificationHub> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
        _redisDb = redis.GetDatabase();
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;

        if (!string.IsNullOrEmpty(userId))
        {
            // Store user-connection mapping in Redis
            await _redisDb.HashSetAsync($"user:{userId}", connectionId, DateTime.UtcNow.ToString());
            await _redisDb.SetAddAsync("online_users", userId);

            _logger.LogInformation($"User {userId} connected with ConnectionId {connectionId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var userId = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;

        if (!string.IsNullOrEmpty(userId))
        {
            // Remove user-connection mapping from Redis
            await _redisDb.HashDeleteAsync($"user:{userId}", connectionId);

            // Check if user has other connections
            var connections = await _redisDb.HashLengthAsync($"user:{userId}");
            if (connections == 0)
            {
                await _redisDb.SetRemoveAsync("online_users", userId);
            }

            _logger.LogInformation($"User {userId} disconnected");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendToUser(string userId, object message)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", message);
    }

    public async Task SendToConnection(string connectionId, object message)
    {
        await Clients.Client(connectionId).SendAsync("ReceiveNotification", message);
    }

    public async Task<IEnumerable<string>> GetOnlineUsers()
    {
        var users = await _redisDb.SetMembersAsync("online_users");
        return users.Select(u => u.ToString());
    }
}