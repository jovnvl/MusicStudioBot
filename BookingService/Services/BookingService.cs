using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Repositories;

namespace BookingService.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;

        public BookingService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<Booking?> CreateBookingAsync(BookingDto createBookingDto, CancellationToken ct)
        {
            var _booking = await GetBookingByDescriptionAsync(createBookingDto.Description, ct);
            if (_booking != null)
                return null;
            return await _bookingRepository.AddBookingAsync(createBookingDto, ct);
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
            var result = await _bookingRepository.GetAllBookingsAsync(ct);
            return result.Where(x => x.Id == id).FirstOrDefault();
        }

        public async Task<Booking?> GetBookingByDescriptionAsync(string name, CancellationToken ct)
        {
            var result = await _bookingRepository.GetAllBookingsAsync(ct);
            return result.Where(x => x.Description == name).FirstOrDefault();
        }

        public async Task<List<Booking>> GetBookingsByRoomIdAsync(int roomId, CancellationToken ct)
        {
            var result = await _bookingRepository.GetAllBookingsAsync(ct);
            return result.Where(x => x.RoomId == roomId).ToList();
        }
        public async Task<List<Booking>> GetBookingsByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var result = await _bookingRepository.GetAllBookingsAsync(ct);
            return result.Where(x => x.UserId == userId).ToList();
        }

    }
}
