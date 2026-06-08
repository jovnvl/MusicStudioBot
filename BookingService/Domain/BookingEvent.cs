using BookingService.Domain.Events;

namespace BookingService.Domain
{
    public sealed record BookingEvent(
    string Level,
    string EventType,
    string Message) : IEvent;

    public sealed record LogEvent(
        string EventType
    ) : IEvent;
    /*
    public sealed class LogEvent : IEvent
    {
        public object EventType { get; private set; }
        public LogEvent( object eventType )
        {
            EventType = eventType;
        }
    }
    */    
}

