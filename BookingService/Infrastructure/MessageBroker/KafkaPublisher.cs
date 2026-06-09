namespace BookingService.Infrastructure.MessageBroker
{
    using System.Text.Json;
    using Confluent.Kafka;

    public sealed class KafkaPublisher
        : IMessagePublisher
    {
        private readonly IProducer<string, string> _producer;

        public KafkaPublisher(
            IProducer<string, string> producer)
        {
            _producer = producer;
        }

        public async Task PublishAsync<T>(
            string topic,
            T message,
            CancellationToken ct)
        {
            var payload =
                JsonSerializer.Serialize(message);

            await _producer.ProduceAsync(
                topic,
                new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = payload
                },
                ct);
        }
    }

}
