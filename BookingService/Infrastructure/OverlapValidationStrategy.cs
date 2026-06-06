using BookingService.DTO;
using BookingService.Infrastructure.Abstraction;
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
            if (booking.Period.TimeBegin >= booking.Period.TimeEnd)
                throw new InvalidOperationException($"TimeBegin: {booking.Period.TimeBegin.Value.ToLocalTime()} must be less than TimeEnd: {booking.Period.TimeEnd.Value.ToLocalTime()}");

            var hasOverlap =
                await _repository.HasOverlappingBookingAsync(
                    booking.RoomId,
                    booking.Period,
                    ct);

            if (hasOverlap)
                throw new InvalidOperationException(
                    $"Уже есть бронь на кабинет {booking.RoomId} на период {booking.Period.TimeBegin?.ToLocalTime()} - {booking.Period.TimeEnd?.ToLocalTime()}");
        }
    }
}
