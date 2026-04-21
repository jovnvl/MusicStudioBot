namespace IdentityService.Models.Entities
{
    public class RefreshToken
    {
        public string Token { get; set; } = null!;
        public Guid UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
