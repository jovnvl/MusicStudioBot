using BookingService.Services;

namespace BookingService.Infrastructure
{
    public sealed class BookingReminderBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BookingReminderBackgroundService(
            IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var bookingService =
                    scope.ServiceProvider
                        .GetRequiredService<IBookingService>();

                var notificationService =
                    scope.ServiceProvider
                        .GetRequiredService<INotificationService>();

                var bookings =
                    await bookingService
                        .GetBookingsStartingWithinHourAsync(DateTime.Now.AddMinutes(1),
                            stoppingToken);

                foreach (var booking in bookings)
                {
                    await notificationService
                        .NotifyBookingReminderAsync(
                            booking,
                            stoppingToken);
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(3),
                    stoppingToken);
            }
        }
    }
}
