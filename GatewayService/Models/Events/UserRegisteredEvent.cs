namespace GatewayService.Models.Events
{
    public class UserRegisteredEvent
    {
        public Guid UserId { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }
}
