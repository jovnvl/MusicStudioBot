using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BookingService.Common;

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
            try
            {
                var bookings = await _bookingService.GetAllBookingsAsync(ct);
                return Ok(bookings);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499); 
            }            
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера",
                });
            }
        }

        [HttpGet("user/{userId:guid}", Name = "GetBookingsByUserIdAsync")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByUserIdAsync(userId, ct);
                return Ok(bookings);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера",
                });
            }
        }

        [HttpGet("room/{roomId:int}", Name = "GetBookingsByRoomIdAsync")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsByRoomIdAsync(int roomId, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByRoomIdAsync(roomId, ct);
                return Ok(bookings);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера",
                });
            }
        }

        [HttpGet("description/{description}", Name = "GetBookingsByDescriptionAsync")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsByDescriptionAsync(string? description, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByDescriptionAsync(description, ct);
                return Ok(bookings);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера",
                });
            }
        }

        [HttpGet("booking/{id:guid}", Name = "GetBookingAsync")]
        [Authorize]
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
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера",
                });
            }
        }
        
        [HttpPost]
        [Authorize(Roles = "Student,Moderator,Administrator")]
        public async Task<ActionResult<Booking>> CreateBookingAsync([FromBody] BookingDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                //await LogToServiceAsync(Constants.ERROR, "validation-problem", "Create Booking validation problem");
                return ValidationProblem(ModelState);
            }
            try
            {
                var booking = await _bookingService.CreateBookingAsync(dto, ct);
                return CreatedAtRoute("GetBookingAsync", new { id = booking?.Id }, booking);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = $"Внутренняя ошибка сервера" });
            }
        }

        [HttpPut]
        [Authorize(Roles = "Student,Moderator,Administrator")]
        public async Task<ActionResult<Booking>> UpdateBookingAsync(BookingDto bookingDto, CancellationToken ct = default)
        {
            try
            {
                var updatedBooking = await _bookingService.UpdateBookingAsync(bookingDto, ct);

                if (!updatedBooking)
                    return BadRequest(new { Message = $"Не удалось обновить бронь {bookingDto.Id}" });
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера",
                });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Student,Moderator,Administrator")]
        public async Task<IActionResult> DeleteBookingAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var deleted = await _bookingService.DeleteBookingAsync(id, ct);
                if (!deleted)
                    return BadRequest(new { Message = $"Не удалось удалить бронь {id} на кaбинет" });
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера",
                });
            }
        }
    }
}
