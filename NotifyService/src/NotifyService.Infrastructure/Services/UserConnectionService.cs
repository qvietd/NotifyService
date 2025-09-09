using System.Collections.Concurrent;

namespace NotifyService.Infrastructure.Services;

public interface IUserConnectionService
{
    void AddConnection(string userId, string connectionId);
    void RemoveConnection(string connectionId);
    bool IsUserOnline(string userId);
    IEnumerable<string> GetUserConnections(string userId);
}

public class UserConnectionService : IUserConnectionService
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
    private readonly ConcurrentDictionary<string, string> _connectionUsers = new();

    public void AddConnection(string userId, string connectionId)
    {
        _userConnections.AddOrUpdate(userId, 
            new HashSet<string> { connectionId },
            (key, connections) =>
            {
                connections.Add(connectionId);
                return connections;
            });

        _connectionUsers[connectionId] = userId;
    }

    public void RemoveConnection(string connectionId)
    {
        if (_connectionUsers.TryRemove(connectionId, out var userId))
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);
                if (!connections.Any())
                {
                    _userConnections.TryRemove(userId, out _);
                }
            }
        }
    }

    public bool IsUserOnline(string userId)
    {
        return _userConnections.ContainsKey(userId) && _userConnections[userId].Any();
    }

    public IEnumerable<string> GetUserConnections(string userId)
    {
        return _userConnections.TryGetValue(userId, out var connections) 
            ? connections.ToList() 
            : Enumerable.Empty<string>();
    }
}