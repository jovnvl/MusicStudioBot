namespace BookingService.Infrastructure.MessageBroker
{
    public interface IRabbitMQPublisher
    {
        Task PublishAsync<T>(string routingKey, T message, CancellationToken ct);
    }
}
