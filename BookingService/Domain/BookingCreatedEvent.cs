using BookingService.Domain.Events;

namespace BookingService.Domain
{
    public sealed record BookingCreatedEvent(
    string Level,
    string EventType,
    string Message) : IEvent;
    public sealed record BookingDeletedEvent(
    string Level,
    string EventType,
    string Message) : IEvent;

    //public sealed record LogEvent(
    //string Level,
    //object EventType) : IEvent;
    public sealed class LogEvent : IEvent
    {
        public object EventType { get; private set; }
        public LogEvent( object eventType )
        {
            EventType = eventType;
        }
    }

    /*
    public sealed class BookingCreatedEvent : IEvent
    {
        public Guid Id { get; }

        public BookingCreatedEvent(Guid id)
        {
            Id = id;
        }
    }
   
    
    public sealed class BookingDeletedEvent : IEvent
    {
        public Guid Id { get; }

        public BookingDeletedEvent(Guid id)
        {
            Id = id;
        }
    }
    */
}

