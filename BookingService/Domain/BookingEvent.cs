using BookingService.Domain.Events;

namespace BookingService.Domain
{
    public sealed record BookingEvent(
    string Level,
    string EventType,
    string Message) : IEvent;
}