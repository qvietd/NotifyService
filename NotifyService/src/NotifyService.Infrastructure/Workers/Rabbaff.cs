using RabbitMQ.Client;

public class RabbitMqConfiguration
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(10);
}

// Main RabbitMQ Service Implementation
public class RabbitMqService : IRabbitMqService1, IDisposable
{
    private readonly ILogger<RabbitMqService> _logger;
    private readonly RabbitMqConfiguration _configuration;
    private IConnection _connection;
    private IModel _channel;
    private readonly object _lock = new object();

    // Retry policy configuration
    private int _maxRetries = 3;
    private TimeSpan _retryDelay = TimeSpan.FromSeconds(5);
    private IAsyncPolicy _retryPolicy;

    // Dead letter configuration
    private string _deadLetterExchange;
    private string _deadLetterRoutingKey;
    private bool _deadLetterEnabled = false;

    public bool IsConnected => _connection?.IsOpen ?? false;

    public RabbitMqService(IOptions<RabbitMqConfiguration> options, ILogger<RabbitMqService> logger)
    {
        _configuration = options.Value;
        _logger = logger;
        _maxRetries = _configuration.MaxRetries;
        _retryDelay = _configuration.RetryDelay;
        ConfigureDefaultRetryPolicy();
    }

    public async Task ConnectAsync()
    {
        try
        {
            if (IsConnected)
            {
                _logger.LogInformation("Already connected to RabbitMQ");
                return;
            }

            await _retryPolicy.ExecuteAsync(async () =>
            {
                lock (_lock)
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = _configuration.HostName,
                        Port = _configuration.Port,
                        UserName = _configuration.UserName,
                        Password = _configuration.Password,
                        VirtualHost = _configuration.VirtualHost,
                        AutomaticRecoveryEnabled = _configuration.AutomaticRecoveryEnabled,
                        NetworkRecoveryInterval = _configuration.NetworkRecoveryInterval,
                        RequestedHeartbeat = TimeSpan.FromSeconds(60),
                        DispatchConsumersAsync = true
                    };

                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    // Set prefetch count for better load distribution
                    _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                    // Register event handlers
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    _logger.LogInformation("Successfully connected to RabbitMQ");
                }
                await Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public void Disconnect()
    {
        try
        {
            lock (_lock)
            {
                if (_channel != null)
                {
                    if (_channel.IsOpen)
                        _channel.Close();
                    _channel.Dispose();
                    _channel = null;
                }

                if (_connection != null)
                {
                    if (_connection.IsOpen)
                        _connection.Close();
                    _connection.Dispose();
                    _connection = null;
                }

                _logger.LogInformation("Disconnected from RabbitMQ");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disconnecting from RabbitMQ");
        }
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        if (!IsConnected)
        {
            await ConnectAsync();
        }

        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                lock (_lock)
                {
                    var properties = _channel.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.DeliveryMode = 2;
                    properties.ContentType = "application/json";
                    properties.ContentEncoding = "utf-8";
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    properties.MessageId = Guid.NewGuid().ToString();

                    _channel.BasicPublish(
                        exchange: exchange,
                        routingKey: routingKey,
                        mandatory: true,
                        basicProperties: properties,
                        body: body
                    );

                    _logger.LogDebug("Published message to exchange: {Exchange}, routing key: {RoutingKey}",
                        exchange, routingKey);
                }
                await Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to exchange: {Exchange}", exchange);
            throw;
        }
    }

    public void Subscribe<T>(string queue, Func<T, Task<bool>> messageHandler)
    {
        if (!IsConnected)
        {
            ConnectAsync().GetAwaiter().GetResult();
        }

        try
        {
            lock (_lock)
            {
                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.Received += async (model, ea) =>
                {
                    var messageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString();

                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var message = JsonSerializer.Deserialize<T>(json);

                        _logger.LogDebug("Processing message {MessageId} from queue: {Queue}",
                            messageId, queue);

                        var result = await messageHandler(message);

                        if (result)
                        {
                            _channel.BasicAck(ea.DeliveryTag, false);
                            _logger.LogDebug("Message {MessageId} acknowledged", messageId);
                        }
                        else
                        {
                            // Requeue the message if handler returns false
                            _channel.BasicNack(ea.DeliveryTag, false, true);
                            _logger.LogWarning("Message {MessageId} not acknowledged, will be requeued",
                                messageId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message {MessageId}", messageId);

                        // Check if we should send to dead letter queue
                        if (_deadLetterEnabled && ea.Redelivered)
                        {
                            // Message has been retried, send to DLQ
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                            _logger.LogWarning("Message {MessageId} sent to dead letter queue", messageId);
                        }
                        else
                        {
                            // Retry the message
                            _channel.BasicNack(ea.DeliveryTag, false, true);
                        }
                    }
                };

                _channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
                _logger.LogInformation("Started consuming messages from queue: {Queue}", queue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to queue: {Queue}", queue);
            throw;
        }
    }

    public void DeclareExchange(string exchangeName, string type = "direct", bool durable = true)
    {
        if (!IsConnected)
        {
            ConnectAsync().GetAwaiter().GetResult();
        }

        try
        {
            lock (_lock)
            {
                _channel.ExchangeDeclare(
                    exchange: exchangeName,
                    type: type,
                    durable: durable,
                    autoDelete: false,
                    arguments: null
                );

                _logger.LogInformation("Declared exchange: {Exchange} of type: {Type}",
                    exchangeName, type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare exchange: {Exchange}", exchangeName);
            throw;
        }
    }

    public void DeclareQueue(string queueName, bool durable = true, bool exclusive = false,
        bool autoDelete = false)
    {
        if (!IsConnected)
        {
            ConnectAsync().GetAwaiter().GetResult();
        }

        try
        {
            lock (_lock)
            {
                var arguments = new Dictionary<string, object>();

                // Add dead letter configuration if enabled
                if (_deadLetterEnabled)
                {
                    arguments["x-dead-letter-exchange"] = _deadLetterExchange;
                    if (!string.IsNullOrEmpty(_deadLetterRoutingKey))
                    {
                        arguments["x-dead-letter-routing-key"] = _deadLetterRoutingKey;
                    }
                }

                _channel.QueueDeclare(
                    queue: queueName,
                    durable: durable,
                    exclusive: exclusive,
                    autoDelete: autoDelete,
                    arguments: arguments.Count > 0 ? arguments : null
                );

                _logger.LogInformation("Declared queue: {Queue}", queueName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare queue: {Queue}", queueName);
            throw;
        }
    }

    public void BindQueue(string queueName, string exchangeName, string routingKey)
    {
        if (!IsConnected)
        {
            ConnectAsync().GetAwaiter().GetResult();
        }

        try
        {
            lock (_lock)
            {
                _channel.QueueBind(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: routingKey,
                    arguments: null
                );

                _logger.LogInformation("Bound queue {Queue} to exchange {Exchange} with routing key {RoutingKey}",
                    queueName, exchangeName, routingKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bind queue {Queue} to exchange {Exchange}",
                queueName, exchangeName);
            throw;
        }
    }

    public void ConfigureRetryPolicy(int maxRetries, TimeSpan delayBetweenRetries)
    {
        _maxRetries = maxRetries;
        _retryDelay = delayBetweenRetries;

        _retryPolicy = Policy
            .Handle<BrokerUnreachableException>()
            .Or<AlreadyClosedException>()
            .Or<Exception>()
            .WaitAndRetryAsync(
                _maxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retry, ctx) =>
                {
                    _logger.LogWarning("Retry {Retry} after {TimeSpan}s due to: {Message}",
                        retry, timeSpan.TotalSeconds, exception.Message);
                });

        _logger.LogInformation("Configured retry policy: Max retries = {MaxRetries}, Delay = {Delay}",
            maxRetries, delayBetweenRetries);
    }

    public void EnableDeadLetterQueue(string deadLetterExchange, string deadLetterRoutingKey)
    {
        _deadLetterExchange = deadLetterExchange;
        _deadLetterRoutingKey = deadLetterRoutingKey;
        _deadLetterEnabled = true;

        // Declare the dead letter exchange
        if (IsConnected)
        {
            DeclareExchange(deadLetterExchange, "direct", true);
        }

        _logger.LogInformation("Enabled dead letter queue with exchange: {Exchange}, routing key: {RoutingKey}",
            deadLetterExchange, deadLetterRoutingKey);
    }

    private void ConfigureDefaultRetryPolicy()
    {
        ConfigureRetryPolicy(_maxRetries, _retryDelay);
    }

    private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection blocked: {Reason}", e.Reason);
    }

    private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "RabbitMQ callback exception");
    }

    private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ connection shutdown: {ReplyText}", e.ReplyText);
    }

    public void Dispose()
    {
        Disconnect();
    }
}
