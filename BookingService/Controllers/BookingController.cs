using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsAsync(CancellationToken ct = default)
        {
            var bookings = await _bookingService.GetAllBookingsAsync(ct);
            return Ok(bookings);
        }

        [HttpGet("user/{userId:guid}", Name = "GetBookingsByUserIdAsync")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            var bookings = await _bookingService.GetBookingsByUserIdAsync(userId, ct);
            return Ok(bookings);
        }

        [HttpGet("room/{roomId:int}", Name = "GetBookingsByRoomIdAsync")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsByRoomIdAsync(int roomId, CancellationToken ct = default)
        {
            var bookings = await _bookingService.GetBookingsByRoomIdAsync(roomId, ct);
            return Ok(bookings);
        }

        [HttpGet("description/{description}", Name = "GetBookingsByDescriptionAsync")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsByDescriptionAsync(string? description, CancellationToken ct = default)
        {
            var bookings = await _bookingService.GetBookingsByDescriptionAsync(description, ct);
            return Ok(bookings);
        }

        [HttpGet("booking/{id:guid}", Name = "GetBookingAsync")]
        [Authorize]
        public async Task<ActionResult<Booking>> GetBookingAsync(Guid id, CancellationToken ct = default)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id, ct);

            if (booking == null)
                return NotFound(new { Message = $"Бронь с ID {id} не найдена" });

            return Ok(booking);
        }
        
        [HttpPost]
        [Authorize(Roles = "Student,Moderator,Administrator")]
        public async Task<ActionResult<Booking>> CreateBookingAsync([FromBody] BookingDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var booking = await _bookingService.CreateBookingAsync(dto, ct);

            return CreatedAtRoute(
                "GetBookingAsync",
                new { id = booking?.Id },
                booking);
        }

        [HttpPut]
        [Authorize(Roles = "Student,Moderator,Administrator")]
        public async Task<ActionResult<Booking>> UpdateBookingAsync(BookingDto bookingDto, CancellationToken ct = default)
        {
            var updatedBooking = await _bookingService.UpdateBookingAsync(bookingDto, ct);

            if (!updatedBooking)
                return BadRequest(new { Message = $"Не удалось обновить бронь {bookingDto.Id}" });

            return Ok();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Student,Moderator,Administrator")]
        public async Task<IActionResult> DeleteBookingAsync(Guid id, CancellationToken ct = default)
        {
            var deleted = await _bookingService.DeleteBookingAsync(id, ct);

            if (!deleted)
                return BadRequest(new { Message = $"Не удалось удалить бронь {id} на кaбинет" });

            return Ok();
        }
    }
}
