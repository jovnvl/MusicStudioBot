using LoggingService.Data;
using LoggingService.Models.Entities;
using LoggingService.Models.Entities.DTO;
using LoggingService.Repositories;

namespace LoggingService.Services
{
    public class LogService : ILogService
    {
        private readonly IRepository _repository;

        public LogService(IRepository repository)
        {
            _repository = repository;
        }

        public async Task AddLogAsync(LogDto logDto, CancellationToken ct)
        {
            await _repository.AddLogAsync(logDto, ct);
        }

        public async Task<IReadOnlyList<Log>> GetLogsAsync(LogFilterDto logFilterDto, CancellationToken ct)
        {
            var logs = await _repository.GetLogsAsync(logFilterDto, ct);
            return logs;
        }
    }
}
