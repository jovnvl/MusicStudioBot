using LoggingService.Models.Entities.DTO;
using LoggingService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

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
        //[HttpGet("health")]
        //public IActionResult Health()
        //{
        //    return Ok(new
        //    {
        //        Status = "Healthy",
        //        RabbitMQ = _rabbitMqHealth.IsHealthy(),
        //        Database = _dbHealth.IsHealthy(),
        //        Timestamp = DateTime.UtcNow
        //    });
        //}

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
