using BookingService.Domain.Events;

namespace BookingService.Infrastructure.Events
{
    public sealed record StatisticEvent(
        string EventType
    ) : IEvent;
    /*
    public sealed class StatisticEvent : IEvent
    {
        public object EventType { get; private set; }
        public LogEvent( object eventType )
        {
            EventType = eventType;
        }
    }
    */
}

