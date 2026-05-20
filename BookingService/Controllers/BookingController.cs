using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Services;
using BookingService.Services.RabbitMQ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        public BookingController(IBookingService bookingService, IRabbitMQPublisher rabbitMQPublisher)
        {
            _bookingService = bookingService;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        private async Task LogToServiceAsync(string level, string eventType, string message)
        {
            await _rabbitMQPublisher.PublishAsync("logging_service_queue", new LogEventDto(level, eventType, message));
        }

        private async Task StatisticToServiceAsync(string eventType)
        {
            await _rabbitMQPublisher.PublishAsync("statistic_service_queue", new { EventType = $"{eventType}"});
        }

        // GET: api/booking
        [HttpGet]
        [Authorize(Roles = "Moderator,Administrator")]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsAsync(CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingService.GetAllBookingsAsync(ct);
                return Ok(bookings);
            }
            
            catch (OperationCanceledException)
            {
                await LogToServiceAsync("Error", "get-bookings-cancel", "Get bookings operation canceled");
                return StatusCode(499); 
            }
            
            catch (Exception ex)
            {
                await LogToServiceAsync("Error", "int-server-error", "Get bookings internal server error");

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                    //Error = ex.Message  GUID ошибки в логгере
                });
            }
        }

        // GET: api/booking/<GUID>
        [HttpGet("{id:guid}", Name = "GetBookingAsync")]
        [Authorize]
        public async Task<ActionResult<Booking>> GetBookingAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id, ct);
                if (booking == null)
                {
                    await LogToServiceAsync("Error", "booking-not-found", $"Booking with ID {id} not found");
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
                await LogToServiceAsync("Error", "int-server-error", "Get booking internal server error");

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                    //Error = ex.Message    GUID ошибки в логгере
                });

            }
        }

        // POST: api/booking
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Booking>> CreateBookingAsync(BookingDto bookingDto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await LogToServiceAsync("Error", "validation-problem", "Create Booking validation problem");
                return ValidationProblem(ModelState);
            }
            try
            {
                var createdBooking = await _bookingService.CreateBookingAsync(bookingDto, ct);
                await LogToServiceAsync("Information", "create-booking", $"New booking {createdBooking.Id} created");
                await StatisticToServiceAsync("CreatedBooking");
                return CreatedAtRoute("GetBookingAsync", new { id = createdBooking.Id }, createdBooking);
            }
            catch (InvalidOperationException ex)
            {
                await LogToServiceAsync("Error", "invalid-operation", "Create booking invalid operation");
                return BadRequest(new { Message = ex.Message });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                await LogToServiceAsync("Error", "int-server-error", "Create booking internal server error");
                return StatusCode(500, new { Message = $"Внутренняя ошибка сервера: {ex}" });
            }
        }

        // DELETE: api/bookings/<GUID>
        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteBookingAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var deleted = await _bookingService.DeleteBookingAsync(id, ct);
                if (!deleted)
                {
                    await LogToServiceAsync("Error", "delete-booking-error", "Booking was not deleted");
                    return BadRequest(new { Message = "Не удалось удалить бронь на кaбинет" });
                }

                await LogToServiceAsync("Information", "delete-booking", $"Booking {id} was deleted");
                await StatisticToServiceAsync("DeletedBooking");
                return Ok();
            }

            catch (OperationCanceledException)
            {
                await LogToServiceAsync("Error", "operation-canceled", "Delete booking operation canceled");
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                await LogToServiceAsync("Error", "int-server-error", "Delete booking internal server error");

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                    //Error = ex.Message    GUID ошибки в логгере
                });

            }
        }
    }
}
