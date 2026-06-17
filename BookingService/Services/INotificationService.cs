using BookingService.Models.Entities;

namespace BookingService.Services
{
    public interface INotificationService
    {
        Task NotifyBookingCreatedAsync(
        Booking booking,
        CancellationToken ct = default);
        Task NotifyBookingUpdatedAsync(
        Booking booking,
        CancellationToken ct = default);
        Task NotifyBookingCancelledAsync(
        Booking booking,
        CancellationToken ct = default);
        Task NotifyBookingReminderAsync(
        Booking booking,
        CancellationToken ct = default);
    }
}
