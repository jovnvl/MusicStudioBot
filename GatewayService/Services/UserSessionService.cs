namespace GatewayService.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly Dictionary<long, string> _sessions = new();
        public string? GetToken(long telegramId)
        {
            if(_sessions.TryGetValue(telegramId, out var token))
                return token;
            return null;
        }

        public void RemoveToken(long telegramId)
        {
            _sessions.Remove(telegramId);
        }

        public void SaveToken(long telegramId, string token)
        {
            _sessions[telegramId] = token;
        }
    }
}
