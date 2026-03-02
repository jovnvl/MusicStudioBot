using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Repositories
{
    public interface IBookingRepository
    {
        Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct);
        Task<Booking> AddBookingAsync(BookingDto createBookingDto, CancellationToken ct);
        Task<bool> RemoveBookingAsync(Guid id, CancellationToken ct);
    }
}
