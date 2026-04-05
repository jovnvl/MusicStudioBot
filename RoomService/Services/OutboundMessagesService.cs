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
     

        public async Task UpdateOutboundMessageAsync(int Id, MessageStatus status, CancellationToken ct)
        {
            await _messageRepository.UpdateStatusAsync(Id, status, ct);
        }
    }
}
