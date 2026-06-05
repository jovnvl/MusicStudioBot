using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;

namespace GatewayService.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IDatabase _redis;
        private const int SessionTTLDays = 30; // как у refresh token

        public UserSessionService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public async Task SaveTokenAsync(long telegramId, string accessToken, string refreshToken)
        {
            var key = $"telegram_session:{telegramId}";
            var value = System.Text.Json.JsonSerializer.Serialize(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });

            await _redis.StringSetAsync(key, value, TimeSpan.FromDays(SessionTTLDays));
        }

        public async Task<(string? accessToken, string? refreshToken)> GetTokensAsync(long telegramId)
        {
            var key = $"telegram_session:{telegramId}";
            var value = await _redis.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return (null, null);

            var session = System.Text.Json.JsonSerializer.Deserialize<SessionData>(value.ToString());
            return (session?.AccessToken, session?.RefreshToken);
        }

        public async Task RemoveTokenAsync(long telegramId)
        {
            await _redis.KeyDeleteAsync($"telegram_session:{telegramId}");
        }

        public Task<bool> IsTokenValidAsync(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return Task.FromResult(jwtToken.ValidTo > DateTime.UtcNow);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private class SessionData
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
        }
    }
}