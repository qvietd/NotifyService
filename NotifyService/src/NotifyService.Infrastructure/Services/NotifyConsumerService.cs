using Microsoft.Extensions.Options;
using NotifyService.Infrastructure.Configuration;
using RabbitMQ.Client;

namespace NotifyService.Infrastructure.Services;

public class NotifyConsumerService : BackgroundService
{
    private readonly ILogger<NotifyConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQConfig _rabbitMQSettings;
    private IConnection _connection;
    private IModel _channel;

    public NotifyConsumerService(
        ILogger<NotifyConsumerService> logger,
        IServiceProvider serviceProvider,
        IOptions<RabbitMQConfig> rabbitMQOptions)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _rabbitMQSettings = rabbitMQOptions.Value;
        InitializeRabbitMQ();
    }

    private void InitializeRabbitMQ()
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMQSettings.HostName,
            UserName = _rabbitMQSettings.UserName,
            Password = _rabbitMQSettings.Password,
            Port = _rabbitMQSettings.Port
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // 1. Khai báo Dead Letter Exchange (DLX) và Dead Letter Queue (DLQ)
        _channel.ExchangeDeclare(_rabbitMQSettings.DeadLetterExchangeName, ExchangeType.Fanout);
        _channel.QueueDeclare(_rabbitMQSettings.DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(_rabbitMQSettings.DeadLetterQueueName, _rabbitMQSettings.DeadLetterExchangeName, "");

        // 2. Khai báo Exchange chính
        _channel.ExchangeDeclare(_rabbitMQSettings.ExchangeName, ExchangeType.Direct, durable: true);

        // 3. Khai báo Queue chính và gắn DLX vào nó
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", _rabbitMQSettings.DeadLetterExchangeName }
        };
        _channel.QueueDeclare(_rabbitMQSettings.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind(_rabbitMQSettings.QueueName, _rabbitMQSettings.ExchangeName, "notification.routing.key");

        _logger.LogInformation("RabbitMQ initialized successfully.");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        // **Prefetch Count**: Chỉ nhận 10 message một lúc để xử lý.
        // Tránh một consumer bị quá tải trong khi các consumer khác đang rảnh.
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received message: {Message}", message);

            try
            {
                // **Retry Policy với Exponential Backoff**
                // Thử lại 5 lần, với thời gian chờ tăng dần (2^n giây)
                var retryPolicy = Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning(ex, "Could not process message. Retrying in {Time}s...", time.TotalSeconds);
                    });

                // Thực thi xử lý message với policy
                await retryPolicy.ExecuteAsync(async () =>
                {
                    // Tạo một scope mới để lấy các service scoped (như DbContext, Repository)
                    using var scope = _serviceProvider.CreateScope();
                    var notificationProcessor = scope.ServiceProvider.GetRequiredService<INotificationProcessor>();
                    await notificationProcessor.ProcessAsync(message);
                });

                // Nếu xử lý thành công, gửi ACK
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message after multiple retries: {Message}", message);
                // **Dead Letter**: Nếu thất bại sau tất cả các lần retry, gửi NACK và không requeue.
                // Message sẽ được chuyển tới DLX -> DLQ.
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: _rabbitMQSettings.QueueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}