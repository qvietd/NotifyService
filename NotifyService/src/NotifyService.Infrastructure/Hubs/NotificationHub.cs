using Microsoft.AspNetCore.SignalR;
using NotifyService.Application.Interfaces;

namespace NotifyService.Infrastructure.Hubs;

public class NotificationHub : Hub
{
    private readonly IConnectionMappingService _connectionMapping;

    public NotificationHub(IConnectionMappingService connectionMapping)
    {
        _connectionMapping = connectionMapping;
    }

    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        await _connectionMapping.AddConnectionAsync(userId, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await _connectionMapping.RemoveConnectionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}