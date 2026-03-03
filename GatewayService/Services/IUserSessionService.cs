namespace GatewayService.Services
{
    public interface IUserSessionService
    {
        void SaveToken(long telegramId, string token);
        string? GetToken(long telegramId);
        void RemoveToken(long telegramId);
    }
}
