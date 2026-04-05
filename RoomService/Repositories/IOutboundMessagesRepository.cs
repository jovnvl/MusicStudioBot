using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public interface IOutboundMessagesRepository
    {
        Task AddOutboundMessageAsync(LogEventDto createOutboundMesssageDto, string QueueName, CancellationToken ct);
        Task UpdateStatusAsync(int id, MessageStatus messageStatus, CancellationToken ct);
    }
}
