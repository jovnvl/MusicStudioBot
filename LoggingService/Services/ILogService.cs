using LoggingService.Models.Entities.DTO;

namespace LoggingService.Services
{
    public interface ILogService
    {
        public Task AddLogAsync(LogDto logDto, CancellationToken ct);
    }
}
