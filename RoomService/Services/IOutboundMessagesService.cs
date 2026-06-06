using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Services
{
    public interface IOutboundMessagesService
    {
        public Task CreateOutboundMessageToLogAsync(LogLevel logLevel, string message, string eventType, bool saveChanges, CancellationToken ct);
        public Task UpdateOutboundMessageAsync(int Id, MessageStatus status, CancellationToken ct);
        public Task<OutboundMessages?> GetActiveOutboundMessagesAsync(CancellationToken ct);
        public Task<List<OutboundMessages>> GetsOutboundMessagesByFilterAsync(LogFilterDto logFilterDto, CancellationToken ct);

    }
}
