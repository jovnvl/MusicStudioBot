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

        public async Task<Booking?> CreateBookingAsync(BookingDto createBookingDto, CancellationToken ct)
        {
            var timeBegin = createBookingDto.TimeBegin ?? DateTime.UtcNow;
            var timeEnd = createBookingDto.TimeEnd ?? timeBegin.AddMinutes(45);

            // Проверка пересечения бронирований
            if (await _bookingRepository.HasOverlappingBookingAsync(createBookingDto.RoomId, timeBegin, timeEnd, ct))
            {
                throw new InvalidOperationException($"Комната {createBookingDto.RoomId} уже забронирована на указанное время");
            }

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                RoomId = createBookingDto.RoomId,
                UserId = createBookingDto.UserId,
                Status = createBookingDto.Status,
                TimeBegin = timeBegin,
                TimeEnd = timeEnd,
                Description = createBookingDto.Description,
                CreationDate = DateTime.UtcNow
            };

            return await _bookingRepository.AddBookingAsync(booking, ct);
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
