using BookingService.Domain.Events;

namespace BookingService.Infrastructure.Events
{
    public sealed record BookingEvent(
    LogLevelType Level,
    string EventType,
    string Message) : IEvent;

    public enum LogLevelType
    {
        Information,
        Warning,
        Error
    }
}