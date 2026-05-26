using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Services
{
    public interface IBookingService
    {
        Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct);
        Task<List<Booking>> GetBookingsByRoomIdAsync(int id, CancellationToken ct);
        Task<Booking?> CreateBookingAsync(BookingDto bookingDto, CancellationToken ct);
        Task<bool> DeleteBookingAsync(Guid id, CancellationToken ct);
        Task<IReadOnlyList<Booking>>GetAllBookingsAsync(CancellationToken ct);
        Task<bool> UpdateBookingAsync(BookingDto bookingDto, CancellationToken ct);
    }
}
