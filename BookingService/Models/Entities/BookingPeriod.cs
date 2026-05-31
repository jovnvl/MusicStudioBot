using System;
namespace BookingService.Models.Entities
{
    public sealed class BookingPeriod
    {
        public DateTime? TimeBegin { get; private set; }
        public DateTime? TimeEnd { get; private set; }
        public BookingPeriod() { }
        public BookingPeriod(DateTime? start, DateTime? end)
        {
            //временно отключим проверку
            //if (end < start) 
            //    throw new ArgumentException("TimeEnd must be greater than TimeBegin");

            TimeBegin = start;
            TimeEnd = end;
        }

        public static BookingPeriod Create(DateTime? start, DateTime? end) => new(start, end);
    }
}
