using System.Collections.Concurrent;
using NotifyService.Application.Interfaces;
using NotifyService.Shared.Models;

namespace NotifyService.Infrastructure.Services;

public interface IBatchProcessor
{
    void AddToBatch(NotificationRequest request);
    void ProcessBatch();
    Task ForceFlushAsync();
}

public class BatchProcessor : IBatchProcessor
{
    private readonly INotificationProcessor _notificationProcessor;
    private readonly ILogger<BatchProcessor> _logger;
    private readonly ConcurrentQueue<NotificationRequest> _batchQueue;
    private readonly Timer _batchTimer;
    private readonly int _batchSize;
    private readonly int _batchTimeoutMs;
    private readonly object _lockObject = new();

    public BatchProcessor(
        INotificationProcessor notificationProcessor,
        IConfiguration configuration,
        ILogger<BatchProcessor> logger)
    {
        _notificationProcessor = notificationProcessor;
        _logger = logger;
        _batchQueue = new ConcurrentQueue<NotificationRequest>();
        
        _batchSize = configuration.GetValue<int>("Batch:MaxSize", 10);
        _batchTimeoutMs = configuration.GetValue<int>("Batch:TimeoutMs", 5000);

        _batchTimer = new Timer(_ => ProcessBatch(), null, _batchTimeoutMs, _batchTimeoutMs);
    }

    public void AddToBatch(NotificationRequest request)
    {
        _batchQueue.Enqueue(request);
        _logger.LogDebug("Added notification to batch queue. Current queue size: {QueueSize}", _batchQueue.Count);

        // Process batch if it reaches the size limit
        if (_batchQueue.Count >= _batchSize)
        {
            _ = Task.Run(ProcessBatch);
        }
    }

    public void ProcessBatch()
    {
        if (_batchQueue.IsEmpty)
            return;

        lock (_lockObject)
        {
            if (_batchQueue.IsEmpty)
                return;

            var batch = new List<NotificationRequest>();

            // Dequeue up to batch size
            while (batch.Count < _batchSize && _batchQueue.TryDequeue(out var notification))
            {
                batch.Add(notification);
            }

            if (batch.Count != 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationProcessor.ProcessBatchNotificationsAsync(batch);
                        _logger.LogInformation("Successfully processed batch of {Count} notifications", batch.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing batch of {Count} notifications", batch.Count);

                        // Re-queue failed items for retry (simple retry mechanism)
                        foreach (var item in batch)
                        {
                            _batchQueue.Enqueue(item);
                        }
                    }
                });
            }
        }
    }

    public async Task ForceFlushAsync()
    {
        while (!_batchQueue.IsEmpty)
        {
            ProcessBatch();
            await Task.Delay(100); // Small delay to prevent tight loop
        }
    }

    public void Dispose()
    {
        _batchTimer?.Dispose();
    }
}