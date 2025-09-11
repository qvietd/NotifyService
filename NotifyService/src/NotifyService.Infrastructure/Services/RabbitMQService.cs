using Microsoft.Extensions.Options;
using NotifyService.Domain.Interfaces;
using NotifyService.Infrastructure.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace NotifyService.Infrastructure.Services;

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly RabbitMQConfig _config;
    private readonly ILogger<RabbitMQService> _logger;
    private IConnection _connection;
    private IModel _channel;
    private EventingBasicConsumer _consumer;

    public RabbitMQService(IOptions<RabbitMQConfig> config, ILogger<RabbitMQService> logger)
    {
        _config = config.Value;
        _logger = logger;
        InitializeRabbitMQ();
    }

    private void InitializeRabbitMQ()
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
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
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

            _channel.QueueBind(_config.NotifyQueue, _config.Exchange, "notify");

            // Declare DLQ
            _channel.ExchangeDeclare(_config.DeadLetterExchange, ExchangeType.Direct, durable: true);
            _channel.QueueDeclare(_config.DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);
            _channel.QueueBind(_config.DeadLetterQueue, _config.DeadLetterExchange, "dlq");

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

    public void StartConsuming(Func<string, Task<bool>> messageHandler)
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
                    AcknowledgeMessage(ea.DeliveryTag);
                }
                else
                {
                    RejectMessage(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                RejectMessage(ea.DeliveryTag, false);
            }
        };

        _channel.BasicConsume(_config.NotifyQueue, false, _consumer);
        _logger.LogInformation("Started consuming messages from RabbitMQ");
    }

    public void AcknowledgeMessage(ulong deliveryTag)
    {
        _channel.BasicAck(deliveryTag, false);
    }

    public void RejectMessage(ulong deliveryTag, bool requeue)
    {
        _channel.BasicReject(deliveryTag, requeue);
    }

    public void PublishToDeadLetter(string message, string error)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var properties = _channel.CreateBasicProperties();
        properties.Headers = new Dictionary<string, object>
            {
                {"error", error},
                {"timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds()}
            };

        _channel.BasicPublish(_config.DeadLetterExchange, "dlq", properties, body);
        _logger.LogWarning($"Message sent to DLQ: {message.Substring(0, Math.Min(100, message.Length))}...");
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}