namespace BookingService.Services.RabbitMQ
{
    public interface IRabbitMQPublisher
    {
        Task PublishAsync<T>(string queueName, T message);

        Task PublishMessageAsync<T>(T sendObject, string queueName, CancellationToken ct);
    }
}
