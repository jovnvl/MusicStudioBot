using IdentityService.Models.Entities;
using System.Text.Json.Serialization;

namespace IdentityService.Models.DTOs
{
    public class ChangePasswordRequest
    {
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }
}
