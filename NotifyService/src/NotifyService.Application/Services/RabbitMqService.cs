using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NotifyService.Application.Interfaces;
using NotifyService.Infrastructure.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotifyService.Application.Services;
public class RabbitMqService : IRabbitMqService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMqSettings _settings;

    public RabbitMqService(IOptions<RabbitMqSettings> settings)
    {
        _settings = settings.Value;

        var factory = new ConnectionFactory()
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public async Task PublishAsync<T>(string queueName, T message)
    {
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: properties, body: body);
        await Task.CompletedTask;
    }

    public async Task StartConsumingAsync<T>(string queueName, Func<T, Task> processMesssage, int prefetchCount = 1)
    {
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)prefetchCount, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json);

                if (message != null)
                {
                    await processMesssage(message);
                }

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error processing message: {ex.Message}");
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}