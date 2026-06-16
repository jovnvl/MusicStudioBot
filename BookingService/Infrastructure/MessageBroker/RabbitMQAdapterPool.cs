using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace BookingService.Infrastructure.MessageBroker
{
    /*
    архитектура вполне рабочая:
    Singleton ConnectionFactory
    ↓
    Singleton RabbitMqChannelPool
    ↓
    Singleton RabbitMQAdapterPool
    */
    public class RabbitMQAdapterPool : IMessagePublisher
    {
        private readonly ILogger<RabbitMQAdapterPool> _logger;
        private readonly RabbitMqChannelPool _channelPool;

        public RabbitMQAdapterPool(
            ILogger<RabbitMQAdapterPool> logger,
            RabbitMqChannelPool channelPool)
        {
            _logger = logger;
            _channelPool = channelPool;
        }

        public async Task PublishAsync<T>(
            string queueName,
            T sendMessage,
            CancellationToken ct)
        {
            IChannel? channel = null;

            try
            {
                channel = await _channelPool.RentAsync(ct);

                var message = JsonSerializer.Serialize(
                    sendMessage,
                    new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder
                            .UnsafeRelaxedJsonEscaping
                    });

                var body = Encoding.UTF8.GetBytes(message);

                var properties = new BasicProperties
                {
                    Persistent = true
                };

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct);

                _logger.LogInformation(
                    "Published message to queue {QueueName}",
                    queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish message to queue {QueueName}",
                    queueName);

                throw;
            }
            finally
            {
                if (channel is not null)
                {
                    _channelPool.Return(channel);
                }
            }
        }
    }
}