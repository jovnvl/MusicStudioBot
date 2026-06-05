namespace GatewayService.Models.DTOs
{
    public class UpdateProfileRequest
    {
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}