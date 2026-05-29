using System.Text.Json.Serialization;

namespace GatewayService.Models.DTOs
{
    public sealed class BookingPeriod
    {
        //[JsonPropertyName("timeBegin")]
        public DateTime? TimeBegin { get; private set; }
        //[JsonPropertyName("timeEnd")]
        public DateTime? TimeEnd { get; private set; }
        public BookingPeriod(DateTime? start, DateTime? end)
        {
            TimeBegin = start;
            TimeEnd = end;
        }
    }
}
