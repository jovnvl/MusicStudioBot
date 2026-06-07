using BookingService.Common;
using BookingService.Domain.Events;
using BookingService.DTO;
using BookingService.Infrastructure.MessageBroker;

namespace BookingService.Domain.Handlers
{
    public sealed class BookingCreatedHandler
    : IEventHandler<BookingCreatedEvent>
    {
        private readonly ILogger<BookingCreatedHandler> _logger;
        private readonly IRabbitMQPublisher _publisher;

        public BookingCreatedHandler(ILogger<BookingCreatedHandler> logger,
            IRabbitMQPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task HandleAsync(
            BookingCreatedEvent @event,
            CancellationToken ct)
        {
            _logger.LogInformation($"Booking: {@event.Message}");

            await _publisher.PublishAsync(Constants.LOGIN_SERVICE_QUEUE,
                new LogEventDto(@event.Level, @event.EventType, @event.Message), ct);
        }
    }

    public sealed class BookingDeletedHandler
    : IEventHandler<BookingDeletedEvent>
    {
        private readonly ILogger<BookingDeletedHandler> _logger;
        private readonly IRabbitMQPublisher _publisher;

        public BookingDeletedHandler(ILogger<BookingDeletedHandler> logger,
            IRabbitMQPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task HandleAsync(
            BookingDeletedEvent @event,
            CancellationToken ct)
        {
            _logger.LogInformation($"Booking: {@event.Message}");

            await _publisher.PublishAsync(Constants.LOGIN_SERVICE_QUEUE, new LogEventDto(@event.Level, @event.EventType, @event.Message), ct);
        }
    }
    public sealed class LogEventHandler
    : IEventHandler<LogEvent>
    {
        private readonly ILogger<LogEventHandler> _logger;
        private readonly IRabbitMQPublisher _publisher;

        public LogEventHandler(ILogger<LogEventHandler> logger,
            IRabbitMQPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task HandleAsync(
            LogEvent @event,
            CancellationToken ct)
        {
            await _publisher.PublishAsync(Constants.STATISTIC_SERVICE_QUEUE,
                 new { EventType = @event.EventType }, ct);
        }
    }
}
