namespace GatewayService.Models.Events
{
    public class UserLoggedInEvent
    {
        public Guid UserId { get; set; }
        public long TelegramId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime LoggedInAt { get; set; }
    }
}
