using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace GatewayService.Services.RabbitMQ
{
    public class RabbitMQPublisher : IRabbitMQPublisher
    {
        private readonly ILogger<RabbitMQPublisher> _logger;
        private readonly string _hostName;

        public RabbitMQPublisher(ILogger<RabbitMQPublisher> logger, IConfiguration configuration)
        {
            _logger = logger;
            _hostName = configuration["RabbitMQ:Host"] ?? "localhost";
        }

        public async Task PublishAsync<T>(string queueName, T message)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = _hostName };
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: false,  // сохраняется ли при перезапуске RabbitMQ
                    exclusive: false,
                    autoDelete: false
                );

                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName,
                    body: body
                );

                _logger.LogInformation("Published message to queue {QueueName}: {Message}", queueName, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to RabbitMQ queue {QueueName}", queueName);
            }
        }
    }
}