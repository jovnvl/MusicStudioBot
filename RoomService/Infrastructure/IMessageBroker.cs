namespace RoomService.Infrastructure
{
    public interface IMessageBroker
    {
        Task PublishMessageAsync<T>(T sendObject, string queueName, CancellationToken ct);
    }
}
