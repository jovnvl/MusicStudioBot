using System;
namespace BookingService.Models.Entities
{
    public sealed class BookingPeriod
    {
        public DateTime? TimeBegin { get; set; }
        public DateTime? TimeEnd { get; set; }
        private BookingPeriod() { }
        public BookingPeriod(DateTime? start, DateTime? end)
        {
            TimeBegin = start;
            TimeEnd = end;
        }

        public static BookingPeriod Create(DateTime? start, DateTime? end) => new(start, end);
    }
}
