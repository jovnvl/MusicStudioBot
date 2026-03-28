using LoggingService.Data;
using LoggingService.Models.Entities;
using LoggingService.Models.Entities.DTO;

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
                Message = logDto.Message,
                Service = logDto.Service,
                Timestamp = logDto.Timestamp
            };
                
            await _dataContext.AddAsync(log, ct);
            await _dataContext.SaveChangesAsync(ct);
        }
    }
}
