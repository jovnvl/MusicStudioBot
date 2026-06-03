using BookingService.Data;
using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Repositories;
using BookingService.Services.RabbitMQ;

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

                if (bookingDto.UserId.ToString() == string.Empty)
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
                await LogToServiceAsync("Error", "invalid-operation", $"ERROR Create booking: {ex.Message}");
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                await LogToServiceAsync("Error", "int-server-error", "Create booking internal server error");
                throw;
            }
        }

        public async Task<bool> UpdateBookingAsync(BookingDto bookingDto, CancellationToken ct = default)
        {
            return await _bookingRepository.UpdateBookingAsync(bookingDto, true, ct = default);
        }

        public async Task<bool> DeleteBookingAsync(Guid id, CancellationToken ct = default)
        {
            return await _bookingRepository.RemoveBookingAsync(id, ct = default);
        }

        public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct = default)
        {
            return await _bookingRepository.GetAllBookingsAsync(ct = default);
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _bookingRepository.GetByIdAsync(id, ct = default);
        }

        public async Task<List<Booking>> GetBookingsByRoomIdAsync(int roomId, CancellationToken ct = default)
        {
            return await _bookingRepository.GetByRoomIdAsync(roomId, ct = default);
        }
        public async Task<List<Booking>> GetBookingsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            return await _bookingRepository.GetByUserIdAsync(userId, ct = default);
        }
        public async Task<List<Booking>> GetBookingsByDescriptionAsync(string? description, CancellationToken ct = default)
        {
            return await _bookingRepository.GetByDescriptionAsync(description, ct = default);
        }

        private async Task LogToServiceAsync(string level, string eventType, string message, CancellationToken ct = default)
        {
            await _rabbitMQPublisher.PublishAsync("logging_service_queue", new LogEventDto(level, eventType, message), ct = default);
        }

        private async Task StatisticToServiceAsync(string eventType, CancellationToken ct = default)
        {
            await _rabbitMQPublisher.PublishAsync("statistic_service_queue", new { EventType = $"{eventType}" }, ct = default);
        }
    }
}
