using BookingService.Data;
using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly DataContext _context;

        public BookingService(IBookingRepository bookingRepository, DataContext context)
        {
            _bookingRepository = bookingRepository;
            _context = context;
        }

        public async Task<Booking?> CreateBookingAsync(BookingDto bookingDto, CancellationToken ct)
        {
            var timeBegin = bookingDto.TimeBegin ?? DateTime.UtcNow;
            var timeEnd = bookingDto.TimeEnd ?? timeBegin.AddMinutes(45);

            // Проверка пересечения бронирований
            if (await _bookingRepository.HasOverlappingBookingAsync(bookingDto.RoomId, timeBegin, timeEnd, ct))
            {
                throw new InvalidOperationException($"Кабинет {bookingDto.RoomId} уже забронирован в данный период времени");
            }

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                RoomId = bookingDto.RoomId,
                UserId = bookingDto.UserId,
                Status = bookingDto.Status,
                TimeBegin = timeBegin,
                TimeEnd = timeEnd,
                Description = bookingDto.Description,
                CreationDate = DateTime.UtcNow
            };

            return await _bookingRepository.AddBookingAsync(booking, ct);
        }

        public async Task<bool> UpdateBookingAsync(BookingDto bookingDto, CancellationToken ct)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var isUpdated = await _bookingRepository.UpdateBookingAsync(bookingDto, false, ct);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(ct);

                return isUpdated;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }

        }

        public async Task<bool> DeleteBookingAsync(Guid id, CancellationToken ct)
        {
            return await _bookingRepository.RemoveBookingAsync(id, ct);
        }

        public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct)
        {
            return await _bookingRepository.GetAllBookingsAsync(ct);
        }

        public async Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct)
        {
            return await _bookingRepository.GetByIdAsync(id, ct);
        }

        public async Task<List<Booking>> GetBookingsByRoomIdAsync(int roomId, CancellationToken ct)
        {
            return await _bookingRepository.GetByRoomIdAsync(roomId, ct);
        }
        public async Task<List<Booking>> GetBookingsByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _bookingRepository.GetByUserIdAsync(userId, ct);
        }

    }
}
