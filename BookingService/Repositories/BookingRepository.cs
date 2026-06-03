using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Models.Mapping;

namespace BookingService.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly DataContext _dataContext;

        public BookingRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<Booking> AddBookingAsync(Booking booking, CancellationToken ct)
        {                  
            await _dataContext.Bookings.AddAsync(booking, ct);
            await _dataContext.SaveChangesAsync(ct);
            return booking;
        }

        public async Task<IReadOnlyList<Booking>> GetAllBookingsAsync(CancellationToken ct)
        {
            var _bookings = await _dataContext.Bookings.ToListAsync(ct);
            /*
            var bookings = await _dataContext.Bookings
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    Begin = x.Period!.TimeBegin,
                    End = x.Period!.TimeEnd
                })
                .ToListAsync();
            
            foreach (var b in bookings)
            {
                Console.WriteLine(
                $"{b.Id} | " +
                $"{b.Begin} | " +
                $"{b.End}"
                );
            }*/
            return _bookings.AsReadOnly();
        }

        public async Task<List<Booking>> GetByDescriptionAsync(string? description, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(description))
                return new List<Booking>();

            return await _dataContext.Bookings
                .Where(b => b.Description != null && b.Description.Contains(description))
                .ToListAsync(ct);
        }

        public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _dataContext.Bookings.FindAsync(new object[] { id }, ct);
        }

        public async Task<List<Booking>> GetByRoomIdAsync(int roomId, CancellationToken ct)
        {
            return await _dataContext.Bookings
                .Where(b => b.RoomId == roomId)
                .ToListAsync(ct);
        }

        public async Task<List<Booking>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            return await _dataContext.Bookings
                .Where(b => b.UserId == userId)
                .ToListAsync(ct);
        }
        public async Task<Booking?> GetBookingByIdAsync(Guid Id, CancellationToken ct)
        {
            var booking = await _dataContext.Bookings.FindAsync(Id, ct);
            return booking;
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

        public async Task<bool> UpdateBookingAsync(BookingDto bookingDto, bool saveChanges, CancellationToken ct)
        {
            var _booking = await GetBookingByIdAsync(bookingDto.Id, ct);

            if (_booking == null)
                return false;

            _booking = BookingMapping.ToEntity(bookingDto);
            if (saveChanges)
                await _dataContext.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> HasOverlappingBookingAsync(int roomId, BookingPeriod? bookingPeriod, CancellationToken ct)
        {
            return await _dataContext.Bookings.AnyAsync(
                b => b.RoomId == roomId
                  && (b.Period != null && bookingPeriod != null
                      && b.Period.TimeBegin < bookingPeriod.TimeEnd
                      && b.Period.TimeEnd > bookingPeriod.TimeBegin)
                  && b.Status != BookingStatus.Canceled,
                ct
            );
        }
    }
}
