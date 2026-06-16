using BookingService.Models.Entities;

namespace BookingService.Infrastructure
{
    public sealed class BookingValidationPipeline
        : IBookingValidationPipeline
    {
        private readonly IEnumerable<IBookingValidationStrategy>
            _strategies;

        public BookingValidationPipeline(
            IEnumerable<IBookingValidationStrategy> strategies)
        {
            _strategies = strategies;
        }

        public async Task ValidateAsync(
            Booking booking,
            CancellationToken ct)
        {
            foreach (var strategy in _strategies)
            {
                await strategy.ValidateAsync(
                    booking,
                    ct);
            }
        }
    }

}
