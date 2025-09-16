using Microsoft.Extensions.Options;
using NotifyService.Infrastructure.Configuration;
using NotifyService.NotifyService.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotifyService.Infrastructure.Services;

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly RabbitMQConfig _config;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly IConnection _connection;
    private IModel _channel;
    private EventingBasicConsumer _consumer;
    private readonly JsonSerializerOptions _jsonOptions;
    private const int maxRetries = 3;
    private const string notifyRoutingKey = "notify";
    private const string deadLetterRoutingKey = "dlq";
    public RabbitMQService(
        IOptions<RabbitMQConfig> config,
        ILogger<RabbitMQService> logger,
        IConnection connection)
    {
        _config = config.Value;
        _logger = logger;
        _connection = connection;
        InitializeRabbitMQ();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
        };
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            _channel = _connection.CreateModel();
            // Declare main exchange and queue
            _channel.ExchangeDeclare(_config.Exchange, ExchangeType.Direct, durable: true);
            var queueArgs = new Dictionary<string, object>
            {
                {"x-dead-letter-exchange", _config.DeadLetterExchange},
                {"x-dead-letter-routing-key", "dlq"}
            };

            _channel.QueueDeclare(_config.NotifyQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            _channel.QueueBind(_config.NotifyQueue, _config.Exchange, notifyRoutingKey);

            // Declare DLQ
            _channel.ExchangeDeclare(_config.DeadLetterExchange, ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(_config.DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);
            _channel.QueueBind(_config.DeadLetterQueue, _config.DeadLetterExchange, deadLetterRoutingKey);

            // Set prefetch count
            _channel.BasicQos(0, (ushort)_config.PrefetchCount, false);

            _logger.LogInformation("RabbitMQ initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ");
            throw;
        }
    }

    public async Task PublishAsync<T>(T @event, string routingKey = "", CancellationToken cancellationToken = default) where T : class
    {
        var retryCount = 0;
        var eventType = @event.GetType();
        var eventTypeName = @event.GetType().Name;
        var message = JsonSerializer.Serialize(@event, eventType, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(message);
        _logger.LogInformation($"Preparing to send event {eventTypeName} with routing key {routingKey}: {message}");
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = retryCount == 0 ? Guid.NewGuid().ToString() : properties.MessageId;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.Headers = new Dictionary<string, object>
        {
            ["EventType"] = eventTypeName,
            ["Source"] = "TodoApi",
            ["Version"] = "1.0",
            ["CorrelationId"] = Guid.NewGuid().ToString(),
            ["RetryCount"] = retryCount
        };
        while (retryCount <= maxRetries)
        {
            try
            {
                _channel.BasicPublish(
                    exchange: _config.Exchange,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Successfully published event {EventName} with routing key {RoutingKey} (attempt {Attempt})",
                    eventTypeName, routingKey, retryCount + 1);

                return; // Success, exit retry loop
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "Failed to publish event {EventType} (attempt {Attempt}/{MaxRetries})",
                    @event.GetType().Name, retryCount, maxRetries + 1);

                if (retryCount > maxRetries)
                {
                    _logger.LogError(ex, "Failed to publish event {EventType} after {MaxRetries} attempts with message: {Message}",
                        @event.GetType().Name, maxRetries + 1, message);
                    throw;
                }

                // Exponential backoff
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * 100), cancellationToken);
            }
        }
    }

    public async Task PublishBatchAsync<T>(IEnumerable<T> messages, string routingKey = "", CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            foreach (var message in messages)
            {
                await PublishAsync(message, routingKey, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch messages");
            throw;
        }
    }

    public async Task StartConsumingAsync(Func<string, Task<bool>> messageHandler, CancellationToken cancellationToken = default)
    {
        _consumer = new EventingBasicConsumer(_channel);
        _consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                var success = await messageHandler(message);
                if (success)
                {
                    await AcknowledgeMessageAsync(ea.DeliveryTag);
                }
                else
                {
                    await RejectMessageAsync(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await RejectMessageAsync(ea.DeliveryTag, false);
            }
        };

        _channel.BasicConsume(_config.NotifyQueue, false, _consumer);
        _logger.LogInformation("Started consuming messages from RabbitMQ");
        await Task.CompletedTask;
    }

    public async Task AcknowledgeMessageAsync(ulong deliveryTag, CancellationToken cancellationToken = default)
    {
        _channel.BasicAck(deliveryTag, false);
        await Task.CompletedTask;
    }

    public async Task RejectMessageAsync(ulong deliveryTag, bool requeue, CancellationToken cancellationToken = default)
    {
        _channel.BasicReject(deliveryTag, requeue);
        await Task.CompletedTask;
    }

    public async Task PublishToDeadLetterAsync(string message, string error, CancellationToken cancellationToken = default)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var properties = _channel.CreateBasicProperties();
        properties.Headers = new Dictionary<string, object>
            {
                {"error", error},
                {"timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds()}
            };

        _channel.BasicPublish(_config.DeadLetterExchange, deadLetterRoutingKey, properties, body);
        _logger.LogWarning($"Message sent to DLQ: {message.Substring(0, Math.Min(100, message.Length))}...");
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}