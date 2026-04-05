using RoomService.DTO;

namespace RoomService.Services
{
    public interface IOutboundMessagesService
    {
        public Task CreateOutboundMessageToLogAsync(LogLevel logLevel, string message, string eventType, CancellationToken ct);
        public Task UpdateOutboundMessageAsync(int Id, MessageStatus status, CancellationToken ct);
 
    }
}
