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

        // POST /api/test/booking — увеличить счетчик
        [HttpPost("deletebooking")]
        public async Task<IActionResult> DeleteBooking(CancellationToken ct)
        {
            await _statisticService.IncrementDeleteBookingCountAsync(ct);

            return Ok(new
            {
                Message = "Бронирование удалено"
            });
        }

        [HttpPost("updatebooking")]
        public async Task<IActionResult> UpdateBooking(CancellationToken ct)
        {
            await _statisticService.IncrementUpdateBookingCountAsync(ct);
            return Ok(new
            {
                Message = "Бронирование обновлено"
            });
        }

        [HttpGet("booking")]
        public async Task<IActionResult> GetBookingCount(CancellationToken ct)
        {
            var total = await _statisticService.GetBookingCountAsync(ct);
            return Ok(new { TotalBookings = total });
        }
    }
}
