using System;
namespace BookingService.Models.Entities
{
    public sealed class BookingPeriod
    {
        public DateTime? TimeBegin { get; }
        public DateTime? TimeEnd { get; }
        public BookingPeriod()
        { 
            TimeBegin = DateTime.UtcNow;
            TimeEnd = DateTime.UtcNow.AddMinutes(45);
        }
        public BookingPeriod(DateTime? start, DateTime? end)
        {
            if (end < start)
                end = start.Value.AddMinutes(45);

            TimeBegin = start;
            TimeEnd = end;
        }

        public static BookingPeriod Create(DateTime? start, DateTime? end) => new(start, end);
    }
}
