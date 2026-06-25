namespace NotificationService.Domain.Events
{
    public class BookingCreatedEvent : IEvent
    {
        public Guid BookingId { get; init; }
        public DateTime CreatedAt { get; init; }
        // + что нужно для вычисления времени напоминания
    }
}
