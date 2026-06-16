using BookingService.Models.Entities;

namespace BookingService.Infrastructure
{
    public interface IBookingValidationPipeline
    {
        Task ValidateAsync(
            Booking booking,
            CancellationToken ct);
    }

}
