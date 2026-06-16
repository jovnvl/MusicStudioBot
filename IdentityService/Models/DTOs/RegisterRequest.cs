namespace IdentityService.Models.DTOs
{
    public class RegisterRequest
    {
        public long TelegramId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Password { get; set; } 
    }
}