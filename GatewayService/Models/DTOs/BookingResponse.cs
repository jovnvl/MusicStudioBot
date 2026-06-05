using GatewayService.Models.Enums;
using System.Text.Json.Serialization;
using Telegram.Bot.Types;

namespace GatewayService.Models.DTOs
{
    public class BookingResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        [JsonPropertyName("user")]
        public User User { get; set; } = null!;

        [JsonPropertyName("roomId")]
        public int RoomId { get; set; }
        //public Room Room { get; set; } = null!;

        [JsonPropertyName("creationDate")]
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("status")]
        public BookingStatus? Status { get; set; } = BookingStatus.NotConfirmed;
        [JsonPropertyName("period")]
        public BookingPeriod Period { get; set; } = new();
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
