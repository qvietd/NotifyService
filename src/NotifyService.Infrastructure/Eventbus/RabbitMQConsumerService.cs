using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using NotifyService.Application.Common.Interfaces;
using NotifyService.Domain.Events;

namespace NotifyService.Infrastructure.EventBus;

public class RabbitMQConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly string _queueName = "todo.notifications";
    private readonly string _exchangeName = "todo.events";
    private readonly JsonSerializerOptions _jsonOptions;
    
    public RabbitMQConsumerService(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };

        var connectionString = configuration.GetConnectionString("RabbitMQ") ?? "amqp://localhost";
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind queue to exchange with routing patterns
            _channel.QueueBind(_queueName, _exchangeName, "todo.todocreatedevent");
            _channel.QueueBind(_queueName, _exchangeName, "todo.todocompletedevent");
            _channel.QueueBind(_queueName, _exchangeName, "todo.todoupdatedevent");
            _channel.QueueBind(_queueName, _exchangeName, "todo.todoreopenedevent");
            _channel.QueueBind(_queueName, _exchangeName, "todo.todoprioritychangedevent");

            _logger.LogInformation("RabbitMQ Consumer Service initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Consumer");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Service started");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var eventType = Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["EventType"]);

                _logger.LogInformation("Received event: {EventType}", eventType);

                await ProcessEventAsync(eventType, message);

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue on error
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessEventAsync(string eventType, string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();



        try
        {
            _logger.LogInformation($"Process mesasage: {message}");
            switch (eventType)
            {
                case "TodoCreatedEvent":
                    var createdEvent = JsonSerializer.Deserialize<TodoCreatedEvent>(message, _jsonOptions);
                    if (createdEvent != null)
                    {
                        await notificationService.SendTodoCreatedNotificationAsync(
                            createdEvent.TodoId,
                            createdEvent.Title);
                    }
                    break;

                case "TodoCompletedEvent":
                    var completedEvent = JsonSerializer.Deserialize<TodoCompletedEvent>(message, _jsonOptions);
                    if (completedEvent != null)
                    {
                        await notificationService.SendTodoCompletedNotificationAsync(
                            completedEvent.TodoId,
                            completedEvent.Title,
                            completedEvent.CompletedAt);
                    }
                    break;

                case "TodoUpdatedEvent":
                    var updatedEvent = JsonSerializer.Deserialize<TodoUpdatedEvent>(message, _jsonOptions);
                    if (updatedEvent != null)
                    {
                        await notificationService.SendTodoUpdatedNotificationAsync(
                            updatedEvent.Id,
                            updatedEvent.NewValue ?? "Updated",
                            $"Changed from '{updatedEvent.OldValue}' to '{updatedEvent.NewValue}'");
                    }
                    break;

                case "TodoReopenedEvent":
                    var reopenedEvent = JsonSerializer.Deserialize<TodoReopenedEvent>(message, _jsonOptions);
                    if (reopenedEvent != null)
                    {
                        await notificationService.SendTodoReopenedNotificationAsync(
                            reopenedEvent.TodoId,
                            reopenedEvent.Title);
                    }
                    break;

                case "TodoPriorityChangedEvent":
                    var priorityEvent = JsonSerializer.Deserialize<TodoPriorityChangedEvent>(message, _jsonOptions);
                    if (priorityEvent != null)
                    {
                        await notificationService.SendTodoPriorityChangedNotificationAsync(
                            priorityEvent.TodoId,
                            "Priority Updated",
                            priorityEvent.NewPriority);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize event: {EventType}", eventType);
        }
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ Consumer");
        }
        finally
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }

        base.Dispose();
    }
}