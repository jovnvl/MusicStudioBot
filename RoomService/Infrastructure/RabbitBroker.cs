using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace RoomService.Infrastructure
{
    public class RabbitBroker : IMessageBroker
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        public RabbitBroker(IConnection connection, IChannel channel)
        {
            _connection = connection;
            _channel = channel;
        }

        public async Task PublishMessageAsync<T>(T sendObject, string queueName, CancellationToken ct)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            var message = JsonSerializer.Serialize(sendObject, options);
            var body = Encoding.UTF8.GetBytes(message);

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                body: body,
                cancellationToken: ct
            );
        }

        public static async Task<RabbitBroker> CreateAsync (string queueName, IConnection connection, IChannel channel)
        {
            await channel.QueueDeclareAsync(queue: queueName,
                                            durable: false,
                                            exclusive: false,
                                            autoDelete: false
                                            );
            return new RabbitBroker(connection, channel);
        }
    }
}
