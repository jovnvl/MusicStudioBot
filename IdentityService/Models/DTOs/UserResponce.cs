namespace IdentityService.Models.DTOs
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}