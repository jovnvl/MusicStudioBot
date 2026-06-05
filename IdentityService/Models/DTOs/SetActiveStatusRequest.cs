using System.Text.Json.Serialization;

namespace IdentityService.Models.DTOs
{
    public class SetActiveStatusRequest
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }
}