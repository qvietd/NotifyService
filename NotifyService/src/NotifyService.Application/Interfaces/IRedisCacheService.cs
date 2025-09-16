public interface IRedisCacheService
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task<T?> GetAsync<T>(string key);
    Task<bool> ExistsAsync(string key);
    Task<bool> RemoveAsync(string key);
    Task<bool> RefreshExpirationAsync(string key, TimeSpan newExpiration);
}