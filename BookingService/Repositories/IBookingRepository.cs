using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Repositories
{
    public interface IBookingRepository
    {
        Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct);
        Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Booking?> GetByDescriptionAsync(string description, CancellationToken ct);
        Task<List<Booking>> GetByRoomIdAsync(int roomId, CancellationToken ct);
        Task<List<Booking>> GetByUserIdAsync(Guid userId, CancellationToken ct);
        Task<Booking> AddBookingAsync(Booking booking, CancellationToken ct);
        Task<bool> RemoveBookingAsync(Guid id, CancellationToken ct);
        Task<bool> HasOverlappingBookingAsync(int roomId, DateTime timeBegin, DateTime timeEnd, CancellationToken ct);
    }
}
