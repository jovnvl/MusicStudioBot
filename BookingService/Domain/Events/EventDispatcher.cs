namespace BookingService.Domain.Events
{
    public sealed class EventDispatcher : IEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public EventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task DispatchAsync<TEvent>(
            TEvent @event,
            CancellationToken ct = default)
            where TEvent : IEvent
        {
            var handlers =
                _serviceProvider.GetServices<IEventHandler<TEvent>>();

            foreach (var handler in handlers)
            {
                await handler.HandleAsync(@event, ct);
            }
        }
    }
}
