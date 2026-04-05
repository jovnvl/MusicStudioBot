using RoomService.Data;
using RoomService.DTO;
using RoomService.Models.Entities;

namespace RoomService.Repositories
{
    public class PgOutboundMessagesRepository : IOutboundMessagesRepository
    {
        private readonly DataContext _dataContext;

        public PgOutboundMessagesRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task AddOutboundMessageAsync(LogEventDto createOutboundMesssageDto, string QueueName,  CancellationToken ct)
        {
            var outboundMessages = new OutboundMessages
            {
                CreatedAt = DateTime.UtcNow,
                EventType = createOutboundMesssageDto.EventType,
                Level = createOutboundMesssageDto.Level,
                Message = createOutboundMesssageDto.Message,
                Service = createOutboundMesssageDto.Service,
                Status = MessageStatus.Active,
                QueueName = QueueName
            };
            await _dataContext.OutboundMessages.AddAsync(outboundMessages, ct);
            await _dataContext.SaveChangesAsync(ct);
        }

        public Task UpdateStatusAsync(int id, MessageStatus messageStatus, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
