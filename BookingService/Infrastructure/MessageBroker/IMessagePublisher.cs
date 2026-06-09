namespace BookingService.Infrastructure.MessageBroker
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(string topic, T message, CancellationToken ct);
    }
}
