using BookingService.Models.Entities;

namespace BookingService.Infrastructure
{
    public interface IBookingValidationStrategy
    {
        Task ValidateAsync(
            Booking booking,
            CancellationToken ct);
    }
}
