using System;
namespace BookingService.Models.Entities
{
    public sealed class BookingPeriod
    {
        public DateTime? TimeBegin { get; set; }
        public DateTime? TimeEnd { get; set; }
        private BookingPeriod() { }
        private BookingPeriod(DateTime? start, DateTime? end)
        {
            TimeBegin = start;
            TimeEnd = end;
        }

        public static BookingPeriod Create(DateTime? start, DateTime? end)
        {
            if (start > end)
                throw new InvalidOperationException(
                    "Start date must be before end date");
            return new BookingPeriod(start, end);
        }
    }
}
