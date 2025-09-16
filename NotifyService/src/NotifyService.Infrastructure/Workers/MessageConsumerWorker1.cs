using Microsoft.Extensions.Options;
using NotifyService.Configurations;
using NotifyService.Converters;
using NotifyService.NotifyService.Core.Entities;
using NotifyService.NotifyService.Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace NotifyService.NotifyService.Infrastructure.Workers;

public class MessageConsumerWorker : BackgroundService
{
    private readonly ILogger<MessageConsumerWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQSetting _config;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly ConcurrentQueue<Notification> _messageQueue;
    private readonly SemaphoreSlim _batchSemaphore;
    private DateTime _lastBatchProcess;
    private readonly INotificationRepository _notificationRepository;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public MessageConsumerWorker(
        ILogger<MessageConsumerWorker> logger,
        IServiceProvider serviceProvider,
        IOptions<RabbitMQSetting> options,
        INotificationRepository notificationRepository)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = options.Value;
        _messageQueue = new ConcurrentQueue<Notification>();
        _batchSemaphore = new SemaphoreSlim(1, 1);
        _lastBatchProcess = DateTime.UtcNow;
        _notificationRepository = notificationRepository;
        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            Converters = { new DictionaryObjectJsonConverter() }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Message Consumer Worker starting...");

        await InitializeRabbitMQ(stoppingToken);

        // Start batch processor task
        _ = Task.Run(async () => await BatchProcessorAsync(stoppingToken), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Message Consumer Worker stopping...");
    }

    private async Task InitializeRabbitMQ(CancellationToken stoppingToken)
    {
        var retryCount = 0;
        const int maxRetries = 5;

        while (retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _config.HostName,
                    Port = _config.Port,
                    UserName = _config.UserName,
                    Password = _config.Password,
                    VirtualHost = _config.VirtualHost,
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Set QoS to control message prefetch
                _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)_config.PrefetchCount, global: false);

                // Declare exchange
                _channel.ExchangeDeclare(
                    exchange: _config.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);

                // Declare queue
                _channel.QueueDeclare(
                    queue: _config.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // Bind queue to exchange
                _channel.QueueBind(
                    queue: _config.QueueName,
                    exchange: _config.ExchangeName,
                    routingKey: _config.RoutingKey);

                // Create consumer
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    await ProcessMessageAsync(ea, stoppingToken);
                };

                consumer.ConsumerCancelled += async (model, ea) =>
                {
                    _logger.LogWarning("Consumer cancelled, attempting to reconnect...");
                    await Task.Delay(5000, stoppingToken);
                    await InitializeRabbitMQ(stoppingToken);
                };

                // Start consuming
                _channel.BasicConsume(
                    queue: _config.QueueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("Successfully connected to RabbitMQ and started consuming messages");
                break;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogError(ex, "Failed to connect to RabbitMQ. Retry {RetryCount}/{MaxRetries}",
                    retryCount, maxRetries);

                if (retryCount < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), stoppingToken);
                }
                else
                {
                    throw new InvalidOperationException("Could not connect to RabbitMQ after maximum retries", ex);
                }
            }
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            var notificationMessage = JsonSerializer.Deserialize<Notification>(message, _jsonSerializerOptions);
            if (notificationMessage != null)
            {
                notificationMessage.DeliveryTag = ea.DeliveryTag;
                _messageQueue.Enqueue(notificationMessage);

                _logger.LogDebug("Message queued for batch processing. Queue size: {QueueSize}",
                    _messageQueue.Count);

                // Check if we should process batch immediately
                if (_messageQueue.Count >= _config.BatchSize)
                {
                    await ProcessBatchAsync(stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            // Reject message and don't requeue
            _channel?.BasicNack(ea.DeliveryTag, false, false);
        }
    }

    private async Task BatchProcessorAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if timeout has passed since last batch
                var timeSinceLastBatch = DateTime.UtcNow - _lastBatchProcess;
                if (timeSinceLastBatch.TotalMilliseconds >= _config.BatchTimeout && !_messageQueue.IsEmpty)
                {
                    await ProcessBatchAsync(stoppingToken);
                }

                await Task.Delay(1000, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch processor");
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        await _batchSemaphore.WaitAsync(stoppingToken);
        try
        {
            if (_messageQueue.IsEmpty)
                return;

            var batch = new List<Notification>();
            var deliveryTags = new List<ulong>();

            // Dequeue up to BatchSize messages
            while (batch.Count < _config.BatchSize && _messageQueue.TryDequeue(out var message))
            {
                batch.Add(message);
                deliveryTags.Add(message.DeliveryTag);
            }

            if (batch.Count == 0)
                return;

            _logger.LogInformation("Processing batch of {BatchSize} messages", batch.Count);
            try
            {
                // Batch insert to MongoDB
                await _notificationRepository.CreateBatchAsync(batch);

                // Acknowledge all messages in batch
                foreach (var tag in deliveryTags)
                {
                    _channel?.BasicAck(tag, false);
                }

                _logger.LogInformation("Successfully processed and saved batch of {Count} notifications",
                    batch.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save batch to MongoDB");

                // Reject all messages and requeue them
                foreach (var tag in deliveryTags)
                {
                    _channel?.BasicNack(tag, false, true);
                }
            }

            _lastBatchProcess = DateTime.UtcNow;
        }
        finally
        {
            _batchSemaphore.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Process remaining messages before stopping
        if (!_messageQueue.IsEmpty)
        {
            _logger.LogInformation("Processing remaining messages before shutdown...");
            await ProcessBatchAsync(cancellationToken);
        }

        _channel?.Close();
        _connection?.Close();

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _batchSemaphore?.Dispose();
        base.Dispose();
    }
}