using Microsoft.AspNetCore.SignalR;
using NotifyService.Infrastructure.Services;

namespace NotifyService.Infrastructure.Hubs;

public class NotificationHub : Hub
{
    private readonly IUserConnectionService _userConnectionService;

    public NotificationHub(IUserConnectionService userConnectionService)
    {
        _userConnectionService = userConnectionService;
    }

    public async Task JoinUser(string userId)
    {
        _userConnectionService.AddConnection(userId, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _userConnectionService.RemoveConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}