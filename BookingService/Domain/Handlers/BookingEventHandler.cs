using BookingService.Common;
using BookingService.Domain.Events;
using BookingService.DTO;
using BookingService.Infrastructure.Events;
using BookingService.Infrastructure.MessageBroker;

namespace BookingService.Domain.Handlers
{
    public sealed class BookingEventHandler
    : IEventHandler<BookingEvent>
    {
        private readonly ILogger<BookingEventHandler> _logger;
        private readonly IRabbitMQPublisher _publisher;

        public BookingEventHandler(ILogger<BookingEventHandler> logger,
            IRabbitMQPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task HandleAsync(
            BookingEvent @event,
            CancellationToken ct)
        {
            _logger.LogInformation($"Booking: {@event.Message}");

            await _publisher.PublishAsync(Constants.LOGIN_SERVICE_QUEUE, new LogEventDto(@event.Level.ToString(), @event.EventType, @event.Message), ct);
        }
    }
}
