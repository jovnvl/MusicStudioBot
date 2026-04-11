using LoggingService.Models.Entities.DTO;
using RoomService.DTO;
using RoomService.Infrastructure;
using RoomService.Models.Entities;
using RoomService.Repositories;

namespace RoomService.Services
{
    public class OutboundMessagesService : IOutboundMessagesService
    {
        private readonly IOutboundMessagesRepository _messageRepository;

        public OutboundMessagesService(IOutboundMessagesRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task CreateOutboundMessageToLogAsync(LogLevel logLevel, string message, string eventType, CancellationToken ct)
        {
            await _messageRepository.AddOutboundMessageAsync(new LogEventDto(logLevel.ToString(), eventType, message), "logging_service_queue", ct);
        }

        public async Task<OutboundMessages?> GetActiveOutboundMessagesAsync(CancellationToken ct)
        {
            return await _messageRepository.GetActiveOtboundMessagesAsync(ct);
        }

        public async Task<List<OutboundMessages>> GetsOutboundMessagesByFilterAsync(LogFilterDto logFilterDto, CancellationToken ct)
        {
            return await _messageRepository.GetOutboundMessagesByFilterAsync(logFilterDto, ct);
        }

        public async Task UpdateOutboundMessageAsync(int Id, MessageStatus status, CancellationToken ct)
        {
            await _messageRepository.UpdateStatusAsync(Id, status, ct);
        }
    }
}
