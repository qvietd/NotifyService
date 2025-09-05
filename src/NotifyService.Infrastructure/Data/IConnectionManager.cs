using System.Collections.Concurrent;

namespace NotifyService.Infrastructure.Data;

public interface IConnectionManager
{
    void AddConnection(string userId, string connectionId);
    void RemoveConnection(string connectionId);
    HashSet<string> GetConnections(string userId);
    bool IsUserOnline(string userId);
}

public class ConnectionManager : IConnectionManager
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> UserConnections = new();

    public void AddConnection(string userId, string connectionId)
    {
        var connections = UserConnections.GetOrAdd(userId, _ => new HashSet<string>());
        lock (connections)
        {
            connections.Add(connectionId);
        }
    }

    public void RemoveConnection(string connectionId)
    {
        foreach (var userId in UserConnections.Keys)
        {
            if (UserConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    if (connections.Contains(connectionId))
                    {
                        connections.Remove(connectionId);
                        if (connections.Count == 0)
                        {
                            UserConnections.TryRemove(userId, out _);
                        }
                        break;
                    }
                }
            }
        }
    }

    public HashSet<string> GetConnections(string userId)
    {
        UserConnections.TryGetValue(userId, out var connections);
        return connections ?? new HashSet<string>();
    }

    public bool IsUserOnline(string userId)
    {
        return UserConnections.ContainsKey(userId) && UserConnections[userId].Any();
    }
}