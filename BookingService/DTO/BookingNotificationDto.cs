using Confluent.Kafka;

namespace BookingService.DTO
{
    public sealed class BookingNotificationDto
    {
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
        public string Service { get; private set; } = "notification-service";

        public string EventType { get; set; } = string.Empty;
        public BookingDto BookingDto  { get; set; }
        public BookingNotificationDto(string eventType,
           BookingDto bookingDto)
        {
            EventType = eventType;
            BookingDto = bookingDto;
        }
    }
}
