using BookingService.Domain.Events;

namespace BookingService.Infrastructure.Events
{
        public sealed record NotificationEvent(
            LogLevelType Level,
            string EventType,
            string Message) : IEvent;
}