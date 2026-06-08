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
            //Нужно применить только если обработчики независимы.
            //await Task.WhenAll(
            //    handlers.Select(h => h.HandleAsync(@event, ct)));

            if (!handlers.Any())
                return;

            foreach (var handler in handlers)
            {
                await handler.HandleAsync(@event, ct);
            }
        }
    }
}
