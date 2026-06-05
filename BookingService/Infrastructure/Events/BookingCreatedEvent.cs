using MediatR;

namespace BookingService.Infrastructure.Events
{
    public class BookingCreatedEvent:  INotification
    {
        public Guid Id { get; }

        public BookingCreatedEvent(Guid id)
        {
            Id = id;
        }
    }
    /*
    public class BookingDeletedEvent : INotification
    {
        public Guid Id { get; }

        public BookingDeletedEvent(Guid id)
        {
            Id = id;
        }
    }
    */
}

