public interface ISignalRService
{
    Task SendToUserAsync(string userId, string method, object message);
    Task SendToGroupAsync(string groupName, string method, object message);
    Task SendToAllAsync(string method, object message);
}