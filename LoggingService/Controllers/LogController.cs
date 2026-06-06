using LoggingService.Models.Entities.DTO;
using LoggingService.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoggingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogController : ControllerBase
    {
        private readonly ILogService _logService;
        public LogController(ILogService logService)
        {
           _logService = logService;
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? service,
        [FromQuery] string? level,
        [FromQuery] string? eventType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
        {
            var logFilterDto = new LogFilterDto()
            {
                eventType = eventType,
                from = from,
                to = to,
                level = level,
                page = page,
                pageSize = pageSize,
                service = service
            };

            var logs = await _logService.GetLogsAsync(logFilterDto, ct);
            return Ok(logs);
        }
    }
}
