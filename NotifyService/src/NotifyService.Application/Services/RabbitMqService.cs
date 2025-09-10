using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NotifyService.Application.Dtos;
using NotifyService.Application.Interfaces;
using NotifyService.Domain.Entities;
using NotifyService.Infrastructure.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotifyService.Application.Services;
public class RabbitMQService : IRabbitMqService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQService> _logger;
    private EventingBasicConsumer _consumer;

    public RabbitMQService(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var factory = new ConnectionFactory()
        {
            Uri = new Uri(_settings.ConnectionString)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        SetupQueuesAndExchange();
    }

    private void SetupQueuesAndExchange()
    {
        // Declare exchange
        _channel.ExchangeDeclare(_settings.ExchangeName, ExchangeType.Direct, true);

        // Declare main queue with DLX
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", $"{_settings.ExchangeName}-dlx" },
            { "x-dead-letter-routing-key", "dead-letter" }
        };

        _channel.QueueDeclare(_settings.QueueName, true, false, false, queueArgs);
        _channel.QueueBind(_settings.QueueName, _settings.ExchangeName, "notification");

        // Declare DLQ exchange and queue
        _channel.ExchangeDeclare($"{_settings.ExchangeName}-dlx", ExchangeType.Direct, true);
        _channel.QueueDeclare(_settings.DeadLetterQueueName, true, false, false);
        _channel.QueueBind(_settings.DeadLetterQueueName, $"{_settings.ExchangeName}-dlx", "dead-letter");

        // Set prefetch count
        _channel.BasicQos(0, (ushort)_settings.PrefetchCount, false);
    }

    public void PublishMessage(NotificationDto notification)
    {
        var json = JsonSerializer.Serialize(notification);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(_settings.ExchangeName, "notification", properties, body);
    }

    public void PublishToDeadLetter(NotificationMessage notification)
    {
        var json = JsonSerializer.Serialize(notification);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish($"{_settings.ExchangeName}-dlx", "dead-letter", properties, body);
    }

    public void StartConsuming(Func<NotificationDto, Task<bool>> messageHandler)
    {
        _consumer = new EventingBasicConsumer(_channel);
        
        _consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            
            try
            {
                var notification = JsonSerializer.Deserialize<NotificationDto>(json);
                var success = await messageHandler(notification);
                
                if (success)
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    _channel.BasicNack(ea.DeliveryTag, false, false); // Send to DLQ
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", json);
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        _channel.BasicConsume(_settings.QueueName, false, _consumer);
    }

    public void StopConsuming()
    {
        _consumer?.Model?.Close();
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}