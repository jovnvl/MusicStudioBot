namespace BookingService.Infrastructure.Abstraction
{
    public interface IEventDispatcher
    {
        Task DispatcherAsync<TEvent>(TEvent @event, CancellationToken ct = default)
            where TEvent : class;
    }
}
