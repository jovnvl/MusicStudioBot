using BookingService.Models.Entities;

namespace BookingService.Infrastructure
{
    public sealed class DateValidationStrategy
        : IBookingValidationStrategy
    {
        public Task ValidateAsync(
            Booking booking,
            CancellationToken ct)
        {
            if (booking.Period.TimeBegin >= booking.Period.TimeEnd)
                //throw new InvalidBookingPeriodException();
                throw new InvalidOperationException($"TimeBegin: {booking.Period.TimeBegin.Value.ToLocalTime()} must be less than TimeEnd: {booking.Period.TimeEnd.Value.ToLocalTime()}");

            return Task.CompletedTask;
        }
    }
}
