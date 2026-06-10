using RabbitMQ.Client;
using System.Threading.Channels;

namespace BookingService.Infrastructure.MessageBroker
{
    public sealed class RabbitMqConnection
    {
        public IConnection? Connection { get; }
        public IChannel? Channel { get; }

        public RabbitMqConnection(IConfiguration configuration)
        {
        }
    }
}
