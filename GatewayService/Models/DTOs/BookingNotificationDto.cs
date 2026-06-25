namespace GatewayService.Models.DTOs
{
    public sealed class BookingNotificationDto
    {
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
        public string Service { get; private set; } = "notification-service";

        public string EventType { get; set; } = string.Empty;
        public BookingRequest BookingDto  { get; set; }
        public BookingNotificationDto(string eventType,
           BookingRequest bookingDto)
        {
            EventType = eventType;
            BookingDto = bookingDto;
        }
    }
}
