using LoggingService.Models.Entities;
using LoggingService.Models.Entities.DTO;

namespace LoggingService.Services
{
    public interface ILogService
    {
        public Task AddLogAsync(LogDto logDto, CancellationToken ct);
        public Task<IReadOnlyList<Log>> GetLogsAsync(LogFilterDto logFilterDto, CancellationToken ct);
    }
}
