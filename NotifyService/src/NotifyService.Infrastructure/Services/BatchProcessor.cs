using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using NotifyService.Application.Interfaces;
using NotifyService.Domain.Entities;
using NotifyService.Infrastructure.Configuration;
using NotifyService.Infrastructure.Repositories;

namespace NotifyService.Application.Services;

public class BatchProcessor : IBatchProcessor, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MongoDBConfig _settings;
    private readonly ILogger<BatchProcessor> _logger;
    private readonly ConcurrentQueue<NotificationMessage> _batch = new();
    private readonly Timer _timer;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public BatchProcessor(
        IServiceProvider serviceProvider,
        IOptions<MongoDBConfig> settings,
        ILogger<BatchProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;

        _timer = new Timer(async _ => await FlushBatchAsync(), null,
            TimeSpan.FromMilliseconds(_settings.BatchTimeoutMs),
            TimeSpan.FromMilliseconds(_settings.BatchTimeoutMs));
    }

    public async Task AddToBatchAsync(NotificationMessage notification)
    {
        _batch.Enqueue(notification);

        if (_batch.Count >= _settings.BatchSize)
        {
            await FlushBatchAsync();
        }
    }

    public async Task FlushBatchAsync()
    {
        if (_batch.IsEmpty) return;

        await _semaphore.WaitAsync();
        try
        {
            var notifications = new List<NotificationMessage>();

            while (notifications.Count < _settings.BatchSize && _batch.TryDequeue(out var notification))
            {
                notifications.Add(notification);
            }

            if (notifications.Any())
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

                await repository.BatchInsertAsync(notifications);
                _logger.LogInformation("Batch inserted {Count} notifications", notifications.Count);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        FlushBatchAsync().Wait();
        _semaphore?.Dispose();
    }
}