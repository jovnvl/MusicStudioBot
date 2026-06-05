namespace RoomService.Services
{
    public interface IMessageBrokerService
    {
        Task SendMessageToLogAsync(string logLevel, string message, string eventType, CancellationToken ct);
    }
}
