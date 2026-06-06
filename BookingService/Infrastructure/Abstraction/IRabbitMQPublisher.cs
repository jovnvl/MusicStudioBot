namespace BookingService.Infrastructure.Abstraction
{
    public interface IRabbitMQPublisher
    {
        Task PublishAsync<T>(string routingKey, T message, CancellationToken ct);
    }
}
