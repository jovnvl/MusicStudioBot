namespace BookingService.Domain.Events
{
    public interface IEventDispatcher<in TEvent> where TEvent : IEvent 
    {
        Task DispatchAsync(TEvent @event, CancellationToken ct = default);
    }
}
