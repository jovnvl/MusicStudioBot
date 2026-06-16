using BookingService.DTO;
using BookingService.Models.Entities;
using BookingService.Repositories;

namespace BookingService.Infrastructure
{
    public sealed class OverlapValidationStrategy
        : IBookingValidationStrategy
    {
        private readonly IBookingRepository _repository;

        public OverlapValidationStrategy(
            IBookingRepository repository)
        {
            _repository = repository;
        }

        public async Task ValidateAsync(
            Booking booking,
            CancellationToken ct)
        {
            var hasOverlap =
                await _repository.HasOverlappingBookingAsync(
                    booking.RoomId,
                    booking.Period,
                    booking.Id,
                    ct);

            if (hasOverlap)
                //throw new BookingOverlapException();
                throw new InvalidOperationException(
                    $"Уже есть бронь на кабинет {booking.RoomId} на период {booking.Period.TimeBegin?.ToLocalTime()} - {booking.Period.TimeEnd?.ToLocalTime()}");
        }
    }
}
