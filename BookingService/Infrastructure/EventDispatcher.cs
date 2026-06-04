namespace BookingService.Infrastructure
{
    public class EventDispatcher : IEventDispatcher
    {
        private readonly IServiceProvider _sp;

        public EventDispatcher(IServiceProvider sp) => _sp = sp;

        public async Task DispatcherAsync<TEvent>(TEvent @event, CancellationToken ct = default)
            where TEvent : class
        {
            var handler = _sp.GetServices<IEventHandler<TEvent>>();
            foreach (var h in handler)
                await h.Handle(@event, ct);
        }
    }
}
