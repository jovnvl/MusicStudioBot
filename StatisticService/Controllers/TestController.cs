using Microsoft.AspNetCore.Mvc;
using StatisticService.Services;

namespace StatisticService.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestStatisticController : ControllerBase
    {
        private readonly IStatisticService _statisticService;

        public TestStatisticController(IStatisticService statisticService)
        {
            _statisticService = statisticService;
        }

        // POST /api/test/booking — увеличить счетчик
        [HttpPost("booking")]
        public async Task<IActionResult> CreateBooking(CancellationToken ct)
        {
            await _statisticService.IncrementBookingCountAsync(ct);
            var total = await _statisticService.GetBookingCountAsync(ct);

            return Ok(new
            {
                Message = "Бронирование создано",
                TotalBookings = total
            });
        }

        // GET /api/test/booking — посмотреть текущее значение
        [HttpGet("booking")]
        public async Task<IActionResult> GetBookingCount(CancellationToken ct)
        {
            var total = await _statisticService.GetBookingCountAsync(ct);
            return Ok(new { TotalBookings = total });
        }
    }
}
