using LoggingService.Models.Entities.DTO;
using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Repositories
{
    public interface IOutboundMessagesRepository
    {
        Task AddOutboundMessageAsync(LogEventDto createOutboundMesssageDto, string QueueName, bool saveChanges, CancellationToken ct);
        Task UpdateStatusAsync(int id, MessageStatus messageStatus, CancellationToken ct);
        public Task<OutboundMessages?> GetActiveOutboundMessagesAsync(CancellationToken ct);
        public Task<List<OutboundMessages>> GetOutboundMessagesByFilterAsync(LogFilterDto logFilterDto, CancellationToken ct);
    }
}
