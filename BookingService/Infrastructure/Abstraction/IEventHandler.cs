namespace BookingService.Infrastructure.Abstraction
{
    public interface IEventHandler<in TEvent>
    {
        Task Handle(TEvent evt, CancellationToken ct);
    }
}
