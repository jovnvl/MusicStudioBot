using BookingService.Data;
using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Repositories;
using BookingService.Services.RabbitMQ;
using BookingService.Common;

namespace BookingService.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;

        public BookingService(IBookingRepository bookingRepository, IRabbitMQPublisher rabbitMQPublisher)
        {
            _bookingRepository = bookingRepository;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        public async Task<Booking?> CreateBookingAsync(BookingDto bookingDto, CancellationToken ct = default)
        {
            try
            {
                if (bookingDto is null)
                    throw new InvalidOperationException("BookingDto is null.");

                // Валидация бизнес-ограничений
                if (bookingDto.TimeBegin >= bookingDto.TimeEnd)
                    throw new InvalidOperationException($"TimeBegin must be less than TimeEnd: ");

                if (bookingDto.UserId == Guid.Empty)
                    throw new InvalidOperationException("Invalid UserId.");

                // Проверка пересечения бронирований
                if (await _bookingRepository.HasOverlappingBookingAsync(bookingDto.RoomId, BookingPeriod.Create(bookingDto.TimeBegin, bookingDto.TimeEnd), ct = default))
                    throw new InvalidOperationException($"Уже есть бронь на кабинет {bookingDto.RoomId} на период {bookingDto.TimeBegin?.ToLocalTime()} - {bookingDto.TimeEnd?.ToLocalTime()}");

                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    RoomId = bookingDto.RoomId,
                    UserId = bookingDto.UserId,
                    Status = bookingDto.Status,
                    Period = BookingPeriod.Create(bookingDto.TimeBegin?.ToUniversalTime(), bookingDto.TimeEnd?.ToUniversalTime()),
                    Description = bookingDto.Description,
                    CreationDate = DateTime.UtcNow
                };

                await _bookingRepository.AddBookingAsync(booking, ct = default);
                await LogToServiceAsync("Information", "create-booking", $"New booking {booking?.Id} for room {booking?.RoomId} on {booking?.Period?.TimeBegin?.ToLocalTime()}-{booking?.Period?.TimeEnd?.ToLocalTime()} created");
                await StatisticToServiceAsync("CreatedBooking");
                return booking;
            }
            catch (InvalidOperationException ex)
            {
                await LogToServiceAsync(Constants.ERROR, "invalid-operation", $"ERROR Create booking: {ex.Message}");
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", "Create booking internal server error");
                throw;
            }
        }

        public async Task<bool> UpdateBookingAsync(BookingDto bookingDto, CancellationToken ct = default)
        {
            try
            {
                var _booking = await _bookingRepository.UpdateBookingAsync(bookingDto, true, ct = default);

                if (!_booking)
                {
                    await LogToServiceAsync(Constants.ERROR, "update-booking-error", "Booking was not updated");
                    throw new InvalidOperationException($"Не удалось обновить бронь {bookingDto.Id}");
                    //return BadRequest(new { Message = "Не удалось обновить бронирование" });
                }
                await LogToServiceAsync("Information", "update-booking", $"Booking {bookingDto.Id} was updated");
                await StatisticToServiceAsync("UpdatedBooking");
                return _booking;
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "operation-canceled", "Update booking operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", $"Update booking internal server error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteBookingAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var _deleted = await _bookingRepository.RemoveBookingAsync(id, ct = default);
                if (!_deleted)
                {
                    await LogToServiceAsync(Constants.ERROR, "delete-booking-error", "Booking was not deleted");
                    throw new InvalidOperationException($"Не удалось удалить бронь {id} на кaбинет");
                }

                await LogToServiceAsync("Information", "delete-booking", $"Booking {id} was deleted");
                await StatisticToServiceAsync("DeletedBooking");
                return _deleted;
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "operation-canceled", "Delete booking operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", $"Delete booking internal server error: {ex.Message}");
                throw;
            }
        }

        public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingRepository.GetAllBookingsAsync(ct = default);
                /*foreach (var b in bookings)
                {
                    Console.WriteLine(
                        $"{b.Id} | {b.Period?.TimeBegin} | {b.Period?.TimeEnd}");
                }*/
                return bookings;
            }

            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "get-bookings-cancel", "Get bookings operation canceled");
                throw;
            }

            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", $"Get bookings internal server error: {ex.Message}");
                throw;      
            }
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var booking = await _bookingRepository.GetByIdAsync(id, ct = default);
                if (booking == null)
                {
                    await LogToServiceAsync(Constants.ERROR, "booking-not-found", $"Booking with ID {id} not found");
                    return null;
                }
                return booking;
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "get-booking-by-Id-cancel", "Get booking by Id operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", $"Get booking internal server error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Booking>> GetBookingsByRoomIdAsync(int roomId, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingRepository.GetByRoomIdAsync(roomId, ct = default);
                return bookings;
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "get-bookings-by-roomId-cancel", "Get bookings by roomId operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", $"Get bookings by roomId internal server error: {ex.Message}");
                throw;
            }
        }
        public async Task<List<Booking>> GetBookingsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingRepository.GetByUserIdAsync(userId, ct = default);
                return bookings;
            }

            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "get-bookings-by-userid-cancel", "Get bookings by userId operation canceled");
                throw;
            }

            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", $"Get bookings by userId internal server error: {ex.Message}");
                throw;
            }
        }
        public async Task<List<Booking>> GetBookingsByDescriptionAsync(string? description, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingRepository.GetByDescriptionAsync(description, ct = default);
                return bookings;
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(Constants.ERROR, "get-bookings-by-description-cancel", "Get bookings by description operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(Constants.ERROR, "int-server-error", $"Get bookings by description internal server error: {ex.Message}");
                throw;
            }
        }

        private async Task LogToServiceAsync(string level, string eventType, string message, CancellationToken ct = default)
        {
            await _rabbitMQPublisher.PublishAsync(Constants.LOGIN_SERVICE_QUEUE, new LogEventDto(level, eventType, message), ct = default);
        }

        private async Task StatisticToServiceAsync(string eventType, CancellationToken ct = default)
        {
            await _rabbitMQPublisher.PublishAsync(Constants.STATISTIC_SERVICE_QUEUE, new { EventType = $"{eventType}" }, ct = default);
        }
    }
}
