using BookingService.Domain.Events;
using BookingService.DTO;

namespace BookingService.Infrastructure.Events
{
        public sealed record NotificationEvent(
            string eventType,
            BookingDto BookingDto
            ) : IEvent;
}