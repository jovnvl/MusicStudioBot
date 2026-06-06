using BookingService.Models.Entities;

namespace BookingService.Infrastructure.Abstraction
{
    public interface IBookingValidationStrategy
    {
        Task ValidateAsync(
            Booking booking,
            CancellationToken ct);
    }
}
