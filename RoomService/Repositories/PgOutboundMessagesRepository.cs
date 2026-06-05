using LoggingService.Models.Entities.DTO;
using Microsoft.EntityFrameworkCore;
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

        public async Task AddOutboundMessageAsync(LogEventDto createOutboundMesssageDto, string QueueName,  bool saveChanges, CancellationToken ct)
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
            if (saveChanges)
            {
                await _dataContext.SaveChangesAsync(ct);
            }
        }

        public async Task<OutboundMessages?> GetActiveOutboundMessagesAsync(CancellationToken ct)
        {
            return await _dataContext.OutboundMessages.Where(x => x.Status == MessageStatus.Active)
                    .OrderBy(x => x.CreatedAt)
                    .FirstOrDefaultAsync(ct);
        }

        public async Task<List<OutboundMessages>> GetOutboundMessagesByFilterAsync(LogFilterDto logFilterDto, CancellationToken ct)
        {
            var query = _dataContext.OutboundMessages.AsQueryable();
            if (logFilterDto.from.HasValue)
                query = query.Where(x => x.CreatedAt >= logFilterDto.from.Value);

            if (logFilterDto.to.HasValue)
                query = query.Where(x => x.CreatedAt >= logFilterDto.to.Value);

            if (!string.IsNullOrEmpty(logFilterDto.service))
                query = query.Where(x => x.Service == logFilterDto.service);

            if (!string.IsNullOrEmpty(logFilterDto.level))
                query = query.Where(x => x.Level == logFilterDto.level);

            if (!string.IsNullOrEmpty(logFilterDto.eventType))
                query = query.Where(x => x.EventType == logFilterDto.eventType);

            var items = await query.OrderByDescending(l => l.CreatedAt)
                                   .Skip((logFilterDto.page - 1) * logFilterDto.pageSize)
                                   .Take(logFilterDto.pageSize)
                                   .ToListAsync();

            return items;
        }

        public async Task UpdateStatusAsync(int id, MessageStatus messageStatus, CancellationToken ct)
        {
            var outboundMessage = await _dataContext.OutboundMessages.Where(x => x.Id == id).FirstOrDefaultAsync(ct);
            if (outboundMessage != null)
            {
                outboundMessage.Status = messageStatus;
                await _dataContext.SaveChangesAsync(ct);
            }
        }
    }
}
