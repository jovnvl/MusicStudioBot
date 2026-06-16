namespace BookingService.Infrastructure.MessageBroker
{
    using System.Text.Json;
    using Confluent.Kafka;

    public sealed class KafkaAdapter
        : IMessagePublisher
    {
        private readonly ILogger<KafkaAdapter> _logger;
        private readonly IProducer<string, string> _producer;

        public KafkaAdapter(
            ILogger<KafkaAdapter> logger,
            IProducer<string, string> producer)
        {
            _logger = logger;
            _producer = producer;
        }

        public async Task PublishAsync<T>(
            string topicName,
            T sendMessage,
            CancellationToken ct)
        {
            try
            {
                var json = JsonSerializer.Serialize(sendMessage);

                var result = await _producer.ProduceAsync(
                    topicName,
                    new Message<string, string>
                    {
                        Value = json
                    },
                    ct);

                _logger.LogInformation(
                    "Published message to topic {Topic}. Partition={Partition}, Offset={Offset}",
                    topicName,
                    result.Partition,
                    result.Offset);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish message to topic {Topic}",
                    topicName);

                throw;
            }
        }
    }

}
