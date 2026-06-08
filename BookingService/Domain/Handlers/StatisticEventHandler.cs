using BookingService.Common;
using BookingService.Domain.Events;
using BookingService.Infrastructure.Events;
using BookingService.Infrastructure.MessageBroker;

namespace BookingService.Domain.Handlers
{
    public sealed class StatisticEventHandler
    : IEventHandler<StatisticEvent>
    {
        private readonly ILogger<StatisticEventHandler> _logger;
        private readonly IRabbitMQPublisher _publisher;

        public StatisticEventHandler(ILogger<StatisticEventHandler> logger,
            IRabbitMQPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task HandleAsync(
            StatisticEvent @event,
            CancellationToken ct)
        {
            await _publisher.PublishAsync(Constants.STATISTIC_SERVICE_QUEUE,
                 new { EventType = @event.EventType }, ct);
        }
    }
}
