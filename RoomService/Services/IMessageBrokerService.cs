namespace RoomService.Services
{
    public interface IMessageBrokerService
    {
        Task SendMessageToLogAsync(LogLevel logLevel, string message, string eventType, CancellationToken ct);
    }
}
