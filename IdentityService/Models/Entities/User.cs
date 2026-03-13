namespace IdentityService.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Student;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public enum UserRole
    {
        Student = 0,
        Moderator = 1,
        Administrator = 2
    }
}