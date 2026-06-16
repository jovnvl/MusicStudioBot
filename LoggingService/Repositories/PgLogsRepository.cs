using LoggingService.Data;
using LoggingService.Models.Entities;
using LoggingService.Models.Entities.DTO;
using Microsoft.EntityFrameworkCore;

namespace LoggingService.Repositories
{
    public class PgLogsRepository : IRepository
    {
        private readonly DataContext _dataContext;

        public PgLogsRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task AddLogAsync(LogDto logDto, CancellationToken ct)
        {
            var log = new Log
            {
                EventType = logDto.EventType,
                Level = logDto.Level,
                Message = logDto.Message.Length > 500 ? logDto.Message.Substring(0, 500) : logDto.Message,
                Service = logDto.Service,
                Timestamp = logDto.Timestamp
            };
                
            await _dataContext.AddAsync(log, ct);
            await _dataContext.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<Log>> GetLogsAsync(LogFilterDto logFilterDto, CancellationToken ct)
        {
            var query = _dataContext.Logs.AsQueryable();
            if (logFilterDto.from.HasValue)
                query = query.Where(x => x.Timestamp >=  logFilterDto.from.Value);
            
            if (logFilterDto.to.HasValue)
                query = query.Where(x => x.Timestamp <= logFilterDto.to.Value);

            if (!string.IsNullOrEmpty(logFilterDto.service))
                query = query.Where(x => x.Service == logFilterDto.service);

            if (!string.IsNullOrEmpty(logFilterDto.level))
                query = query.Where(x => x.Level == logFilterDto.level);

            if (!string.IsNullOrEmpty(logFilterDto.eventType))
                query = query.Where(x => x.EventType == logFilterDto.eventType);

            var items = await query.OrderByDescending(l => l.Timestamp)
                                   .Skip((logFilterDto.page - 1) * logFilterDto.pageSize)
                                   .Take(logFilterDto.pageSize)
                                   .ToListAsync();

            return items;

        }
    }
}
