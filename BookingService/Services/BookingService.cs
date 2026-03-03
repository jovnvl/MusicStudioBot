using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Repositories;

namespace BookingService.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IRoomService _roomService;
        private readonly ICategoryRoomService _categoryRoomService;
        private readonly IAuthService _authService;

        public BookingService(IBookingRepository bookingRepository, IRoomService roomService, ICategoryRoomService categoryRoomService, IAuthService authService)
        {
            _bookingRepository = bookingRepository;
            _roomService = roomService;
            _categoryRoomService = categoryRoomService;
            _authService = authService;
        }

        public async Task<Booking?> CreateBookingAsync(BookingDto createBookingDto, CancellationToken ct)
        {
            var _categoryExists = await _categoryRoomService.ExistsCategoryRoomAsync(createBookingDto.Room.CategoryRoomId, ct);
            if (!_categoryExists)
                return null;
            var _roomExists = await _roomService.ExistsRoomAsync(createBookingDto.Room.CategoryRoomId, ct);
            if (!_roomExists)
                return null;

            var _userExists = await _authService.ExistsUserAsync(createBookingDto.User.Id, ct);
            if (!_userExists)
                return null;

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
            return result.Where(x => x.Room.Id == roomId).ToList();
        }
    }
}
