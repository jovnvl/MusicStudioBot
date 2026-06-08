using BookingService.Infrastructure.Events;
using System.Diagnostics;

namespace BookingService.DTO
{
    public class LogEventDto
    {
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
        public string Service { get; private set; } = "booking-service";
        public string Level { get; private set; }
        public string EventType { get; private set; }
        public string Message { get; private set; } 

        public LogEventDto(string level, string eventType, string message   )
        {
            Level = level; 
            EventType = eventType;
            Message = message;
        }
    }
    
    
}
