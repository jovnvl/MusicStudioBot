using GatewayService.Models.Enums;
using System.Text.Json.Serialization;

namespace GatewayService.Models.DTOs
{
    public class RoomResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("photo")]
        public byte[]? Photo { get; set; }
        [JsonPropertyName("creationDate")]
        public DateOnly CreationDate { get; set; }
        [JsonPropertyName("categoryRoomId")]
        public int CategoryRoomId { get; set; }
        [JsonPropertyName("status")]
        public RoomStatus? Status { get; set; }
    }
}
