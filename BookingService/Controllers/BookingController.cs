using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Services;

namespace BookingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // GET: api/bookings
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsAsync(CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingService.GetAllBookingsAsync(ct);
                return Ok(bookings);
            }
            
            catch (OperationCanceledException)
            {
                return StatusCode(499); 
            }
            
            catch (Exception ex)
            {
                // Добавить логирование ошибки

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                    //Error = ex.Message  GUID ошибки в логгере
                });
            }
        }

        // GET: api/booking/<GUID>
        [HttpGet("{id:guid}", Name = "GetBookingAsync")]
        public async Task<ActionResult<Booking>> GetBookingAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id, ct);
                if (booking == null)
                {
                    return NotFound(new { Message = $"Бронь с ID {id} не найдена" });
                }
                return Ok(booking);
            }
            
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            
            catch (Exception ex)
            {
                // Добавить логирование ошибки

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                    //Error = ex.Message    GUID ошибки в логгере
                });

            }
        }

        // POST: api/bookings
        [HttpPost]
        public async Task<ActionResult<Booking>> CreateBookingAsync(BookingDto bookingDto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);
            try
            {
                var createdBooking = await _bookingService.CreateBookingAsync(bookingDto, ct);
                return CreatedAtRoute("GetBookingAsync", new { id = createdBooking.Id }, createdBooking);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                // TODO: Логирование
                return StatusCode(500, new { Message = $"Внутренняя ошибка сервера: {ex}" });
            }
        }

        // DELETE: api/bookings/<GUID>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteBookingAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                //var exists = await _bookingsService.ExistsBookingAsync(id, ct);
                //if (!exists)
                //{
                //    return NotFound(new { Message = $"Кабинет с ID {id} не найден" });
                //}

                var deleted = await _bookingService.DeleteBookingAsync(id, ct);
                if (!deleted)
                {
                    return BadRequest(new { Message = "Не удалось удалить бронь на кaбанет" });
                }

                return Ok();
            }

            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                // Добавить логирование ошибки

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                    //Error = ex.Message    GUID ошибки в логгере
                });

            }
        }
    }
}
