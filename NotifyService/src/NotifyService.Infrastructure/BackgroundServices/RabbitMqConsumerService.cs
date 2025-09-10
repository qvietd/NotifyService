using System.Text;
using System.Text.Json;
using NotifyService.Application.Interfaces;
using NotifyService.Infrastructure.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotifyService.Infrastructure.BackgroundServices;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly IBatchProcessor _batchProcessor;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumerService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<RabbitMqConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
        using (var scope = serviceProvider.CreateScope())
        {
            _batchProcessor = scope.ServiceProvider.GetRequiredService<IBatchProcessor>();
        }
        InitializeRabbitMq();
    }

    private void InitializeRabbitMq()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:HostName"] ?? "localhost",
                Port = _configuration.GetValue<int>("RabbitMq:Port", 5672),
                UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMq:VirtualHost"] ?? "/"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            var queueName = _configuration["RabbitMq:QueueName"] ?? "notifications";
            var exchangeName = _configuration["RabbitMq:ExchangeName"] ?? "notifications.exchange";

            // Declare exchange and queue
            _channel.ExchangeDeclare(exchangeName, ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queueName, exchangeName, "notification");

            // Set QoS to control message prefetch
            var prefetchCount = _configuration.GetValue<ushort>("RabbitMq:PrefetchCount", 10);
            _channel.BasicQos(0, prefetchCount, false);

            _logger.LogInformation("RabbitMQ initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
        {
            _logger.LogError("RabbitMQ channel is not initialized");
            return;
        }

        var queueName = _configuration["RabbitMq:QueueName"] ?? "notifications";
        var consumer = new EventingBasicConsumer(_channel);

        // Determine processing mode: batch or individual
        var enableBatchProcessing = _configuration.GetValue<bool>("Processing:EnableBatchProcessing", true);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogDebug("Received message: {Message}", message);

                // Try to deserialize as single notification first
                var singleNotification = TryDeserializeSingle(message);
                if (singleNotification != null)
                {
                    if (enableBatchProcessing)
                    {
                        // Add to batch for processing
                        _batchProcessor.AddToBatch(singleNotification);
                        _logger.LogDebug("Added single notification to batch for user {UserId}", singleNotification.UserId);
                    }
                    else
                    {
                        // Process immediately
                        using var scope = _serviceProvider.CreateScope();
                        var processor = scope.ServiceProvider.GetRequiredService<INotificationProcessor>();
                        await processor.ProcessNotificationAsync(singleNotification);
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    // Try to deserialize as batch of notifications
                    var batchNotifications = TryDeserializeBatch(message);
                    if (batchNotifications != null && batchNotifications.Any())
                    {
                        if (enableBatchProcessing)
                        {
                            // Add all to batch processor
                            foreach (var notification in batchNotifications)
                            {
                                _batchProcessor.AddToBatch(notification);
                            }
                            _logger.LogDebug("Added {Count} notifications to batch", batchNotifications.Count);
                        }
                        else
                        {
                            // Process batch immediately
                            using var scope = _serviceProvider.CreateScope();
                            var processor = scope.ServiceProvider.GetRequiredService<INotificationProcessor>();
                            await processor.ProcessBatchNotificationsAsync(batchNotifications);
                        }

                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize notification message: {Message}", message);
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Started consuming messages from queue: {QueueName} with batch processing: {BatchEnabled}",
            queueName, enableBatchProcessing);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQ consumer service is stopping");

            // Flush any remaining batched notifications
            await _batchProcessor.ForceFlushAsync();
        }
    }

    private NotificationRequest? TryDeserializeSingle(string message)
    {
        try
        {
            return JsonSerializer.Deserialize<NotificationRequest>(message);
        }
        catch
        {
            return null;
        }
    }

    private List<NotificationRequest>? TryDeserializeBatch(string message)
    {
        try
        {
            return JsonSerializer.Deserialize<List<NotificationRequest>>(message);
        }
        catch
        {
            return null;
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}