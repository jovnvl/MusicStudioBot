namespace BookingService.Domain.Events
{
    public sealed class EventDispatcher<TEvent> : IEventDispatcher<TEvent>
         where TEvent : IEvent
    {
        private readonly IEnumerable<IEventHandler<TEvent>> _handlers;
        
        public EventDispatcher(IEnumerable<IEventHandler<TEvent>> handlers)
        {
            _handlers = handlers;
        }

        public async Task DispatchAsync(
            TEvent @event,
            CancellationToken ct = default)
        {            
            //Нужно применить только если обработчики независимы.
            //await Task.WhenAll(
            //    _handlers.Select(h => h.HandleAsync(@event, ct)));

            if (!_handlers.Any())
                return;

            foreach (var _handler in _handlers)
            {
                await _handler.HandleAsync(@event, ct);
            }
        }
    }
}
