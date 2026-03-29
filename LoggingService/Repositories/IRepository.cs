using LoggingService.Models.Entities;
using LoggingService.Models.Entities.DTO;

namespace LoggingService.Repositories
{
    public interface IRepository
    {
        public Task AddLogAsync(LogDto log, CancellationToken ct);
        public Task<IReadOnlyList<Log>> GetLogsAsync(LogFilterDto logFilterDto, CancellationToken ct);
    }
}
