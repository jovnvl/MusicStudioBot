namespace GatewayService.Models.DTOs
{
    public class LoginRequest
    {
        public long TelegramId { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}
