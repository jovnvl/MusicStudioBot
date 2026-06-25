using BookingService.Data;
using BookingService.Models.Entities;

namespace BookingService.Services
{
    public sealed class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;


        private readonly DataContext _db;

        public NotificationService(ILogger<NotificationService> logger, DataContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task NotifyBookingCreatedAsync(Booking booking, CancellationToken ct = default)
        {
            _logger.LogInformation("NotifyBookingCreatedAsync: BookingId={BookingId}", booking.Id);
            await Task.CompletedTask;
        }

        public async Task NotifyBookingUpdatedAsync(Booking booking, CancellationToken ct)
        {
            _logger.LogInformation("NotifyBookingUpdatedAsync: BookingId={BookingId}", booking.Id);
            await Task.CompletedTask;
        }

        public async Task NotifyBookingCancelledAsync(Booking booking, CancellationToken ct)
        {
            _logger.LogInformation("NotifyBookingCancelledAsync: BookingId={BookingId}", booking.Id);
            await Task.CompletedTask;
        }

        public async Task NotifyBookingReminderAsync(Booking booking, CancellationToken ct)
        {
            _logger.LogInformation("NotifyBookingReminderAsync: BookingId={BookingId}", booking.Id);
            await Task.CompletedTask;
        }
    }
}
