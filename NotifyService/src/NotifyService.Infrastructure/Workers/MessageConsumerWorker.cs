using Microsoft.Extensions.Options;
using NotifyService.Domain.Entities;
using NotifyService.Domain.Interfaces;
using NotifyService.Infrastructure.Configuration;
using NotifyService.Infrastructure.Repositories;
using System.Collections.Concurrent;
using System.Text.Json;

namespace NotifyService.Infrastructure.Workers;

public class MessageConsumerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageConsumerWorker> _logger;
    private readonly MongoDBConfig _mongoConfig;
    private readonly RabbitMQConfig _rabbitConfig;
    private readonly ConcurrentQueue<NotificationMessage> _messageBuffer;
    private readonly SemaphoreSlim _batchSemaphore;
    private DateTime _lastBatchTime;

    public MessageConsumerWorker(
        IServiceProvider serviceProvider,
        ILogger<MessageConsumerWorker> logger,
        IOptions<MongoDBConfig> mongoConfig,
        IOptions<RabbitMQConfig> rabbitConfig)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _mongoConfig = mongoConfig.Value;
        _rabbitConfig = rabbitConfig.Value;
        _messageBuffer = new ConcurrentQueue<NotificationMessage>();
        _batchSemaphore = new SemaphoreSlim(1, 1);
        _lastBatchTime = DateTime.UtcNow;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MessageConsumerWorker started");

        using var scope = _serviceProvider.CreateScope();
        var rabbitMQService = scope.ServiceProvider.GetRequiredService<IRabbitMQService>();
        var messageRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        // Start batch processing task after delay 5s (based on config), and queue not full
        _ = Task.Run(async () => await ProcessBatchPeriodically(messageRepository, stoppingToken), stoppingToken);

        // Start consuming messages
        rabbitMQService.StartConsuming(async (message) =>
        {
            try
            {
                var notifyMessage = JsonSerializer.Deserialize<NotificationMessage>(message);
                if (notifyMessage == null)
                {
                    _logger.LogWarning("Failed to deserialize message");
                    return false;
                }
                _messageBuffer.Enqueue(notifyMessage);

                // Check if we should process batch
                if (_messageBuffer.Count >= _mongoConfig.BatchSize)
                {
                    await ProcessBatch(messageRepository);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message: {message}");

                // Check retry count and send to DLQ if exceeded
                if (ShouldSendToDeadLetter(message))
                {
                    rabbitMQService.PublishToDeadLetter(message, ex.Message);
                }

                return false;
            }
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessBatchPeriodically(INotificationRepository repository, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_messageBuffer.Count > 0 &&
                    (DateTime.UtcNow - _lastBatchTime).TotalMilliseconds >= _mongoConfig.BatchTimeoutMs)
                {
                    await ProcessBatch(repository);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in periodic batch processing");
            }
            await Task.Delay(_mongoConfig.BatchTimeoutMs, stoppingToken);
        }
    }

    private async Task ProcessBatch(INotificationRepository repository)
    {
        await _batchSemaphore.WaitAsync();
        try
        {
            var messages = new List<NotificationMessage>();
            while (_messageBuffer.TryDequeue(out var message) && messages.Count < _mongoConfig.BatchSize)
            {
                messages.Add(message);
            }

            if (messages.Any())
            {
                var success = await repository.BatchInsertAsync(messages);
                if (success)
                {
                    _logger.LogInformation($"Successfully inserted batch of {messages.Count} messages");
                }
                else
                {
                    // Re-queue messages on failure
                    foreach (var msg in messages)
                    {
                        _messageBuffer.Enqueue(msg);
                    }
                }
            }

            _lastBatchTime = DateTime.UtcNow;
        }
        finally
        {
            _batchSemaphore.Release();
        }
    }

    private bool ShouldSendToDeadLetter(string message)
    {
        try
        {
            var msg = JsonSerializer.Deserialize<NotificationMessage>(message);
            return msg?.RetryCount >= _rabbitConfig.MaxRetryCount;
        }
        catch
        {
            return true;
        }
    }
}