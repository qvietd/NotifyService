using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using NotifyService.Application.Common.Interfaces;

namespace NotifyService.Infrastructure.EventBus;

public class RabbitMQEventBusService : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQEventBusService> _logger;
    private readonly string _exchangeName = "todo.events";
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMQEventBusService(IConfiguration configuration, ILogger<RabbitMQEventBusService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
        };

        var connectionString = configuration.GetConnectionString("RabbitMQ") ?? "amqp://localhost";
        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(60)
        };

        try
        {
            _connection = factory.CreateConnection("TodoApi-Publisher");
            _channel = _connection.CreateModel();

            // Declare exchange with error handling
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare dead letter exchange for failed messages
            _channel.ExchangeDeclare(
                exchange: $"{_exchangeName}.deadletter",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("Enhanced RabbitMQ EventBus connected to {ConnectionString}", connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount <= maxRetries)
        {
            try
            {
                var eventType = @event.GetType();
                var eventTypeName = @event.GetType().Name;
                var routingKey = $"todo.{eventTypeName.ToLowerInvariant()}";

                var message = JsonSerializer.Serialize(@event, eventType, _jsonOptions);
                var body = Encoding.UTF8.GetBytes(message);
                _logger.LogInformation($"Preparing event {eventTypeName} with routing key {routingKey}: {message}");
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.Headers = new Dictionary<string, object>
                {
                    ["EventType"] = eventTypeName,
                    ["Source"] = "TodoApi",
                    ["Version"] = "1.0",
                    ["CorrelationId"] = Guid.NewGuid().ToString()
                };

                _channel.BasicPublish(
                    exchange: _exchangeName,
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
                    _logger.LogError(ex, "Failed to publish event {EventType} after {MaxRetries} attempts",
                        @event.GetType().Name, maxRetries + 1);
                    throw;
                }

                // Exponential backoff
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * 100), cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing Enhanced RabbitMQ EventBus");
        }
        finally
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}