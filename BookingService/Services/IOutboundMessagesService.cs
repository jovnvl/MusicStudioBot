using LoggingService.Models.Entities.DTO;
using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Services
{
    public interface IOutboundMessagesService
    {
        public Task CreateOutboundMessageToLogAsync(LogLevel logLevel, string message, string eventType, bool saveChanges, CancellationToken ct);
        public Task UpdateOutboundMessageAsync(int Id, MessageStatus status, CancellationToken ct);
        public Task<OutboundMessages?> GetActiveOutboundMessagesAsync(CancellationToken ct);
        public Task<List<OutboundMessages>> GetsOutboundMessagesByFilterAsync(LogFilterDto logFilterDto, CancellationToken ct);

    }
}
