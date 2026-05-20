using System.Text.Json.Serialization;

namespace GatewayService.Models.DTOs
{
    public class AuthResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
    }
}
