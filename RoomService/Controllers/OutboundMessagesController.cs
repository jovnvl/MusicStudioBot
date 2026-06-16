using Microsoft.AspNetCore.Mvc;
using RoomService.DTO;
using RoomService.Services;

namespace RoomService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OutboundMessagesController : ControllerBase
    {
        private readonly IOutboundMessagesService _outboundMessagesService;

        public OutboundMessagesController(IOutboundMessagesService outboundMessagesService)
        {
            _outboundMessagesService = outboundMessagesService;
        }

        [HttpGet]
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

            var logs = await _outboundMessagesService.GetsOutboundMessagesByFilterAsync(logFilterDto, ct);
            return Ok(logs);
        }
    }
}
