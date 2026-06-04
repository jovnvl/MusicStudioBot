namespace BookingService.Infrastructure
{
    public interface IEventDispatcher
    {
        Task DispatcherAsync<TEvent>(TEvent @event, CancellationToken ct = default)
            where TEvent : class;
    }
}
