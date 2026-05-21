using LoggingService.Models.Entities.DTO;
using Microsoft.AspNetCore.Mvc;
using BookingService.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BookingService.Controllers
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

        // GET api/<ValuesController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            throw new NotImplementedException();
        }

        // PUT api/<ValuesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
            throw new NotImplementedException();
        }
    }
}
