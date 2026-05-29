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

        public BookingService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<Booking?> CreateBookingAsync(BookingDto bookingDto, CancellationToken ct)
        {           
            // Проверка пересечения бронирований
            if (await _bookingRepository.HasOverlappingBookingAsync(bookingDto.RoomId, BookingPeriod.Create(bookingDto.TimeBegin, bookingDto.TimeEnd), ct))
            {
                throw new InvalidOperationException($"Кабинет {bookingDto.RoomId} уже забронирован в данный период времени");
            }

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                RoomId = bookingDto.RoomId,
                UserId = bookingDto.UserId,
                Status = bookingDto.Status,
                Period = BookingPeriod.Create( bookingDto.TimeBegin, bookingDto.TimeEnd),
                Description = bookingDto.Description,
                CreationDate = DateTime.UtcNow
            };

            return await _bookingRepository.AddBookingAsync(booking, ct);
        }

        public async Task<bool> UpdateBookingAsync(BookingDto bookingDto, CancellationToken ct)
        {
                return await _bookingRepository.UpdateBookingAsync(bookingDto, true, ct);
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
        public async Task<List<Booking>> GetBookingsByDescriptionAsync(string? description, CancellationToken ct)
        {
            return await _bookingRepository.GetByDescriptionAsync(description, ct);
        }

    }
}
