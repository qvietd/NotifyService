using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace NotifyService.NotifyService.Api.HealthCheck;

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly IConnection _connection;

    public RabbitMQHealthCheck(IConnection connection)
    {
        _connection = connection;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connection.IsOpen)
                return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ is healthy"));

            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ connection is closed"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"RabbitMQ check failed: {ex.Message}"));
        }
    }
}

public class MongoDBHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;

    public MongoDBHealthCheck(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync<object>("{ping:1}", cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"MongoDB check failed: {ex.Message}");
        }
    }
}

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            db.Ping();
            return Task.FromResult(HealthCheckResult.Healthy("Redis is healthy"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Redis check failed: {ex.Message}"));
        }
    }
}