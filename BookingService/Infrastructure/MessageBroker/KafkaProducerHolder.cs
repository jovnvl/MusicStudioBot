using Confluent.Kafka;

namespace BookingService.Infrastructure.MessageBroker
{
    //Поскольку IProducer буферизует сообщения, при остановке приложения желательно вызвать Flush.
    //зарегистрировать обертку и уже ее хранить как Singleton:
    //builder.Services.AddSingleton<KafkaProducerHolder>();
    public sealed class KafkaProducerHolder : IDisposable
    {
        public IProducer<string, string> Producer { get; }

        public KafkaProducerHolder(IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers =
                    configuration["Kafka:BootstrapServers"]
            };

            Producer = new ProducerBuilder<string, string>(config)
                .Build();
        }

        public void Dispose()
        {
            Producer.Flush(TimeSpan.FromSeconds(10));
            Producer.Dispose();
        }
    }
}
