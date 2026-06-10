using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace BookingService.Infrastructure.MessageBroker
{
    public class RabbitMQAdapter : IMessagePublisher
    {
        private readonly ILogger<RabbitMQAdapter> _logger;
        private readonly string _hostName;

        private readonly ConnectionFactory _factory;

        private IConnection? _connection;
        private IChannel? _channel;

        private readonly SemaphoreSlim _publishLock = new(1, 1);

        public RabbitMQAdapter(ILogger<RabbitMQAdapter> logger, RabbitMqConnection rabbitMqConnection, IConfiguration configuration)
        {
            _logger = logger;
            _hostName = configuration["RabbitMQ:Host"] ?? "localhost";
            _factory = new ConnectionFactory { HostName = _hostName };
            _connection = rabbitMqConnection.Connection;
            _channel = rabbitMqConnection.Channel;
        }

        private async Task EnsureConnectedAsync(CancellationToken ct)
        {
            if (_connection != null && _channel != null && _connection.IsOpen && _channel.IsOpen)
                return;

            // Синхронизация на установку соединения
            await _publishLock.WaitAsync(ct);
            try
            {
                if (_connection != null && _channel != null && _connection.IsOpen && _channel.IsOpen)
                    return;

                _connection = await _factory.CreateConnectionAsync(ct);
                _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
            }
            finally
            {
                _publishLock.Release();
            }
        }

        public async Task PublishAsync<T>(string queueName, T sendMessage, CancellationToken ct)
        {
            try
            {
                await EnsureConnectedAsync(ct);

                // У IChannel нет потокобезопасности => сериализуем публикацию
                await _publishLock.WaitAsync(ct);
                try
                {
                    if (_channel == null)
                        throw new InvalidOperationException("RabbitMQ channel is not initialized.");

                    await _channel.QueueDeclareAsync(
                        queue: queueName,
                        durable: false,
                        exclusive: false,
                        autoDelete: false,
                        cancellationToken: ct);

                    var options = new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = true
                    };

                    var message = JsonSerializer.Serialize(sendMessage, options);
                    var body = Encoding.UTF8.GetBytes(message);

                    await _channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: queueName,
                        body: body,
                        cancellationToken: ct
                        );

                    _logger.LogInformation("Published message to queue {QueueName}: {Message}", queueName, message);
                }
                finally
                {
                    _publishLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to RabbitMQ queue {QueueName}", queueName);
                throw;
            }
        }
    }
}
