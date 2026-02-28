using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.DTO;
using BookingService.Models.Entities;

namespace BookingService.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly DataContext _dataContext;

        public BookingRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<Booking> AddBookingAsync(BookingDto createBookingDto, CancellationToken ct)
        {
            var booking = new Booking(room: createBookingDto.Room, user: createBookingDto.User,
                status: createBookingDto.Status, 
                timeBegin: DateTime.UtcNow,
                timeEnd: DateTime.UtcNow.AddMinutes(60),
                description: createBookingDto.Description, creationDate: DateTime.UtcNow)
            {
                Id = Guid.NewGuid(),
            };
            await _dataContext.Bookings.AddAsync(booking, ct);
            await _dataContext.SaveChangesAsync(ct);
            return booking;
        }

        public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct)
        {
            var _bookings = await _dataContext.Bookings.ToListAsync(ct);
            return _bookings.AsReadOnly();
        }

        public async Task<bool> RemoveBookingAsync(Guid id, CancellationToken ct)
        {
            var _booking = await _dataContext.Bookings.FindAsync(id, ct);

            if (_booking == null)
                return false;

            _dataContext.Bookings.Remove(_booking);
            await _dataContext.SaveChangesAsync(ct);

            return true;
        }
    }
}
