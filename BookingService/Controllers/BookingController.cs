using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Services;
using BookingService.Services.RabbitMQ;
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
        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        public BookingController(IBookingService bookingService, IRabbitMQPublisher rabbitMQPublisher)
        {
            _bookingService = bookingService;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        private async Task LogToServiceAsync(string level, string eventType, string message, CancellationToken ct = default)
        {
            await _rabbitMQPublisher.PublishAsync(Constants.LOGIN_SERVICE_QUEUE, new LogEventDto(level, eventType, message), ct);
        }

        private async Task StatisticToServiceAsync(string eventType, CancellationToken ct = default)
        {
            await _rabbitMQPublisher.PublishAsync(Constants.STATISTIC_SERVICE_QUEUE, new { EventType = $"{eventType}"}, ct);
        }

        // GET: api/booking
        [HttpGet]
        //[Authorize(Roles = "Moderator,Administrator")]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsAsync(CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingService.GetAllBookingsAsync(ct);
                /*foreach (var b in bookings)
                {
                    Console.WriteLine(
                        $"{b.Id} | {b.Period?.TimeBegin} | {b.Period?.TimeEnd}");
                }*/

                /*return Ok(new
                {
                    Count = bookings.Count,
                    First = bookings.FirstOrDefault()
                });*/
                return Ok(bookings);
            }

            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "get-bookings-cancel", "Get bookings operation canceled");
                return StatusCode(499); 
            }
            
            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", "Get bookings internal server error");

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                });
            }
        }

        // GET: api/booking
        [HttpGet("user/{userId:guid}", Name = "GetBookingsByUserIdAsync")]
        //[Authorize(Roles = "Moderator,Administrator")]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByUserIdAsync(userId, ct);
                return Ok(bookings);
            }

            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "get-bookings-by-userid-cancel", "Get bookings by userId operation canceled");
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", "Get bookings by userId internal server error");

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                });
            }
        }

        // GET: api/booking
        [HttpGet("room/{roomId:int}", Name = "GetBookingsByRoomIdAsync")]
        //[Authorize(Roles = "Moderator,Administrator")]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsByRoomIdAsync(int roomId, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByRoomIdAsync(roomId, ct);
                return Ok(bookings);
            }

            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "get-bookings-by-roomId-cancel", "Get bookings by roomId operation canceled");
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", "Get bookings by roomId internal server error");

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                });
            }
        }

        // GET: api/booking
        [HttpGet("booking/{description}", Name = "GetBookingsByDescriptionAsync")]
        //[Authorize(Roles = "Moderator,Administrator")]
        public async Task<ActionResult<IReadOnlyList<Booking>>> GetBookingsByDescriptionAsync(string? description, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingService.GetBookingsByDescriptionAsync(description, ct);
                return Ok(bookings);
            }

            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "get-bookings-by-description-cancel", "Get bookings by description operation canceled");
                return StatusCode(499);
            }

            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", "Get bookings by description internal server error");

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                });
            }
        }

        // GET: api/booking/<GUID>
        [HttpGet("booking/{id:guid}", Name = "GetBookingAsync")]
        //[Authorize]
        public async Task<ActionResult<Booking>> GetBookingAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var booking = await _bookingService.GetBookingByIdAsync(id, ct);
                if (booking == null)
                {
                    await LogToServiceAsync(Constants.ERROR, "booking-not-found", $"Booking with ID {id} not found");
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
                await LogToServiceAsync(Constants.ERROR, "int-server-error", "Get booking internal server error");

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                });

            }
        }
        
        [HttpPost]
        //[Authorize]
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
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Внутренняя ошибка сервера: {ex}" });
            }
        }

        // UPDATE: api/bookings/<GUID>
        [HttpPut]
        //[Authorize]
        public async Task<ActionResult<Booking>> UpdateBookingAsync(BookingDto bookingDto, CancellationToken ct = default)
        {
            try
            {
                var updatedBooking = await _bookingService.UpdateBookingAsync(bookingDto, ct);

                if (!updatedBooking)
                {
                    await LogToServiceAsync(Constants.ERROR, "update-booking-error", "Booking was not updated");
                    return BadRequest(new { Message = "Не удалось обновить бронирование" });

                }
                await LogToServiceAsync("Information", "update-booking", $"Booking {bookingDto.Id} was updated");
                await StatisticToServiceAsync("UpdatedBooking");

                return Ok();
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "operation-canceled", "Update booking operation canceled");
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", "Update booking internal server error");

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                });
            }
        }

        // DELETE: api/bookings/<GUID>
        [HttpDelete("{id:guid}")]
        //[Authorize]
        public async Task<IActionResult> DeleteBookingAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var deleted = await _bookingService.DeleteBookingAsync(id, ct);
                if (!deleted)
                {
                    await LogToServiceAsync(Constants.ERROR, "delete-booking-error", "Booking was not deleted");
                    return BadRequest(new { Message = "Не удалось удалить бронь на кaбинет" });
                }

                await LogToServiceAsync("Information", "delete-booking", $"Booking {id} was deleted");
                await StatisticToServiceAsync("DeletedBooking");
                return Ok();
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "operation-canceled", "Delete booking operation canceled");
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", "Delete booking internal server error");

                return StatusCode(500, new
                {
                    Message = $"Внутренняя ошибка сервера\n{ex.Message}",
                });
            }
        }
    }
}
