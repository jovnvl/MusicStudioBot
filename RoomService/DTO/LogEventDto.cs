using System.Diagnostics;

namespace RoomService.DTO
{
    public class LogEventDto
    {
        public DateTime Timestamp { get; private set; }
        public string Service { get; private set; }
        public string Level { get; private set; }
        public string EventType { get; private set; }
        public string Message { get; private set; } 
        //public Guid CorrelationId;
        //public string Metadata;
        //public DurationMs = stopwatch.ElapsedMilliseconds,
        public LogEventDto(string level, string eventType, string message   )
        {
            Timestamp = DateTime.UtcNow;
            Service = "room-service";
            Level = level; 
            EventType = eventType;
            Message = message;
        }
    }
    
    
}
