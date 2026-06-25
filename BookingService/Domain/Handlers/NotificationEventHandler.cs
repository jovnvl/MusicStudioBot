using BookingService.Common;
using BookingService.Domain.Events;
using BookingService.DTO;
using BookingService.Infrastructure.Events;
using BookingService.Infrastructure.MessageBroker;

namespace BookingService.Domain.Handlers
{
    public sealed class NotificationEventHandler
    : IEventHandler<NotificationEvent>
    {
        private readonly ILogger<NotificationEventHandler> _logger;
        private readonly IMessagePublisher _publisher;

        public NotificationEventHandler(ILogger<NotificationEventHandler> logger,
            IMessagePublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task HandleAsync(
            NotificationEvent @event,
            CancellationToken ct)
        {
            await _publisher.PublishAsync(Constants.NOTIFICATION_SERVICE_QUEUE,
                  new BookingNotificationDto(
                      eventType: @event.eventType,
                      bookingDto: @event.BookingDto
                      ), ct);
        }
    }
}
