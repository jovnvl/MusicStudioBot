namespace BookingService.Domain.Events
{
    public interface IEventDispatcher
    {
        Task DispatchAsync<TEvent>(
            TEvent @event,
            CancellationToken ct = default)
            where TEvent : IEvent;
    }
}
