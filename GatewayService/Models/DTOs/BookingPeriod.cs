using System.Text.Json.Serialization;

namespace GatewayService.Models.DTOs
{
    public sealed class BookingPeriod
    {
        [JsonPropertyName("timeBegin")]
        public DateTime? TimeBegin { get; set; }
        [JsonPropertyName("timeEnd")]
        public DateTime? TimeEnd { get; set; }
        public BookingPeriod() { }
        public BookingPeriod(DateTime? start, DateTime? end)
        {
            TimeBegin = start;
            TimeEnd = end;
        }
    }
}
