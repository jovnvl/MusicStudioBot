using BookingService.Infrastructure.Abstraction;
using MediatR;

namespace BookingService.Infrastructure
{
    public sealed class MediatorRDispatcher : IEventDispatcher
    {
        private readonly IMediator _mediator;
        public MediatorRDispatcher(IMediator mediator) => _mediator = mediator;

        public Task DispatcherAsync<TEvent>(TEvent @event, CancellationToken ct = default)
            where TEvent : class
            => _mediator.Publish(@event, ct);
    }
}
