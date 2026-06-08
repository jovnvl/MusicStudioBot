using BookingService.Common;
using BookingService.Data;
using BookingService.Domain;
using BookingService.Domain.Events;
using BookingService.DTO;
using BookingService.Infrastructure;
using BookingService.Infrastructure.Concurrency;
using BookingService.Infrastructure.Events;
using BookingService.Infrastructure.MessageBroker;
using BookingService.Models.Entities;
using BookingService.Models.Mapping;
using BookingService.Repositories;

namespace BookingService.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<BookingService> _logger;

        private readonly IEventDispatcher _dispatcher;
        private readonly IBookingValidationPipeline _validationPipeline;

        private readonly IBookingLockProvider _lockProvider;

        public BookingService(IBookingRepository bookingRepository, IBookingValidationPipeline validationPipeline,
            IEventDispatcher dispatcher, ILogger<BookingService> logger, IBookingLockProvider lockProvider)
        {
            _bookingRepository = bookingRepository;
            _dispatcher = dispatcher;
            _logger = logger;
            _validationPipeline = validationPipeline;
            _lockProvider = lockProvider;
        }
        public async Task<Booking?> CreateBookingAsync(BookingDto bookingDto, CancellationToken ct = default)
        {
            try
            {
                if (bookingDto is null)
                    throw new InvalidOperationException("BookingDto is null.");
                if (bookingDto.UserId == Guid.Empty)
                    throw new InvalidOperationException("Invalid UserId.");

                var booking = Booking.Create
                (
                    roomId: bookingDto.RoomId,
                    userId: bookingDto.UserId,
                    status: bookingDto.Status,
                    period: BookingPeriod.Create(bookingDto.TimeBegin?.ToUniversalTime(), bookingDto.TimeEnd?.ToUniversalTime()),
                    description: bookingDto.Description
                );

                var semaphore = _lockProvider.GetLock(bookingDto.RoomId);
                await semaphore.WaitAsync(ct);
                try
                {
                    await _validationPipeline.ValidateAsync(bookingDto.ToEntity(), ct);
                    await _bookingRepository.AddBookingAsync(booking, ct);
                }
                finally
                {
                    semaphore.Release();
                }

                await LogToServiceAsync(LogLevelType.Information, "create-booking", $"New booking {booking?.Id} for room {booking?.RoomId} on {booking?.Period?.TimeBegin?.ToLocalTime()}-{booking?.Period?.TimeEnd?.ToLocalTime()} created");
                                
                await StatisticToServiceAsync("CreatedBooking");
                return booking;
            }
            catch (InvalidOperationException ex)
            {
                await LogToServiceAsync(LogLevelType.Error, "invalid-operation", $"ERROR Create booking: {ex.Message}");
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                await LogToServiceAsync(LogLevelType.Error, "int-server-error", "Create booking internal server error");
                throw;
            }
        }

        public async Task<bool> UpdateBookingAsync(BookingDto bookingDto, CancellationToken ct = default)
        {
            try
            {
                bool _updateBooking = false;
                var semaphore = _lockProvider.GetLock(bookingDto.RoomId);
                await semaphore.WaitAsync(ct);
                try
                {

                    await _validationPipeline.ValidateAsync(bookingDto.ToEntity(), ct);

                    _updateBooking = await _bookingRepository.UpdateBookingAsync(bookingDto, true, ct);
                }
                finally
                {
                    semaphore.Release();
                }

                if (!_updateBooking)
                {
                    await LogToServiceAsync(LogLevelType.Error, "update-booking-error", "Booking was not updated");
                    throw new InvalidOperationException($"Не удалось обновить бронь {bookingDto.Id}");
                    //return BadRequest(new { Message = "Не удалось обновить бронирование" });
                }
                await LogToServiceAsync(LogLevelType.Information, "update-booking", $"Booking {bookingDto.Id}  for room {bookingDto?.RoomId} on {bookingDto?.TimeBegin?.ToLocalTime()}-{bookingDto?.TimeEnd?.ToLocalTime()} was updated");

                await StatisticToServiceAsync("UpdatedBooking");
                return _updateBooking;
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(LogLevelType.Error, "operation-canceled", "Update booking operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(LogLevelType.Error, "int-server-error", $"Update booking internal server error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteBookingAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var _deleted = await _bookingRepository.RemoveBookingAsync(id, ct);
                if (!_deleted)
                {
                    await LogToServiceAsync(LogLevelType.Error, "delete-booking-error", "Booking was not deleted");
                    throw new InvalidOperationException($"Не удалось удалить бронь {id} на кaбинет");
                }

                await LogToServiceAsync(LogLevelType.Information, "delete-booking", $"Booking {id} was deleted");

                await StatisticToServiceAsync("DeletedBooking");
                return _deleted;
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(LogLevelType.Error, "operation-canceled", "Delete booking operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(LogLevelType.Error, "int-server-error", $"Delete booking internal server error: {ex.Message}");
                throw;
            }
        }

        public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingRepository.GetAllBookingsAsync(ct);
                /*
                foreach (var b in bookings)
                {
                    _logger.LogInformation($"Booking created with Id ${ b.Id} | { b.Period?.TimeBegin} | { b.Period?.TimeEnd}");;
                    //Console.WriteLine($"{b.Id} | {b.Period?.TimeBegin} | {b.Period?.TimeEnd}");
                }*/
                return bookings;
            }

            catch (OperationCanceledException)
            {
                await LogToServiceAsync(LogLevelType.Error, "get-bookings-cancel", "Get bookings operation canceled");
                throw;
            }

            catch (Exception ex)
            {
                await LogToServiceAsync(LogLevelType.Error, "int-server-error", $"Get bookings internal server error: {ex.Message}");
                throw;      
            }
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var booking = await _bookingRepository.GetByIdAsync(id, ct);
                if (booking == null)
                {
                    await LogToServiceAsync(LogLevelType.Error, "booking-not-found", $"Booking with ID {id} not found");
                    return null;
                }
                return booking;
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(LogLevelType.Error, "get-booking-by-Id-cancel", "Get booking by Id operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(LogLevelType.Error, "int-server-error", $"Get booking internal server error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Booking>> GetBookingsByRoomIdAsync(int roomId, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingRepository.GetByRoomIdAsync(roomId, ct);
                return bookings;
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(LogLevelType.Error, "get-bookings-by-roomId-cancel", "Get bookings by roomId operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(LogLevelType.Error, "int-server-error", $"Get bookings by roomId internal server error: {ex.Message}");
                throw;
            }
        }
        public async Task<List<Booking>> GetBookingsByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingRepository.GetByUserIdAsync(userId, ct);
                return bookings;
            }

            catch (OperationCanceledException)
            {
                await LogToServiceAsync(LogLevelType.Error, "get-bookings-by-userid-cancel", "Get bookings by userId operation canceled");
                throw;
            }

            catch (Exception ex)
            {
                await LogToServiceAsync(LogLevelType.Error, "int-server-error", $"Get bookings by userId internal server error: {ex.Message}");
                throw;
            }
        }
        public async Task<List<Booking>> GetBookingsByDescriptionAsync(string? description, CancellationToken ct = default)
        {
            try
            {
                var bookings = await _bookingRepository.GetByDescriptionAsync(description, ct);
                return bookings;
            }
            catch (OperationCanceledException)
            {
                await LogToServiceAsync(LogLevelType.Error, "get-bookings-by-description-cancel", "Get bookings by description operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                await LogToServiceAsync(LogLevelType.Error, "int-server-error", $"Get bookings by description internal server error: {ex.Message}");
                throw;
            }
        }

        private async Task LogToServiceAsync(LogLevelType level, string eventType, string message, CancellationToken ct = default)
        {
            await _dispatcher.DispatchAsync(new BookingEvent(
                level,
                eventType,
                message),
                ct);
           // await _rabbitMQPublisher.PublishAsync(Constants.LOGIN_SERVICE_QUEUE, new LogEventDto(level, eventType, message), ct);
        }

        
        private async Task StatisticToServiceAsync(string eventType, CancellationToken ct = default)
        {
            await _dispatcher.DispatchAsync(new StatisticEvent(eventType), ct);
            //await _rabbitMQPublisher.PublishAsync(Constants.STATISTIC_SERVICE_QUEUE, new { EventType = $"{eventType}" }, ct);
        }
    }
}
