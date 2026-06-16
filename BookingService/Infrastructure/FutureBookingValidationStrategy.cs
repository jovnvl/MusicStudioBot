using BookingService.Models.Entities;

namespace BookingService.Infrastructure
{
    public sealed class FutureBookingValidationStrategy
        : IBookingValidationStrategy
    {
        public Task ValidateAsync(
            Booking booking,
            CancellationToken ct)
        {
            if (booking.Period.TimeBegin <= DateTime.UtcNow)
                //throw new BookingInPastException();
                throw new InvalidOperationException($"TimeBegin: {booking.Period.TimeBegin.Value.ToLocalTime()} must be greater than the current date.");

            return Task.CompletedTask;
        }
    }

}
