namespace GatewayService.Services
{
    public interface IUserSessionService
    {
        Task SaveTokenAsync(long telegramId, string accessToken, string refreshToken);
        Task<(string? accessToken, string? refreshToken)> GetTokensAsync(long telegramId);
        Task RemoveTokenAsync(long telegramId);
        Task<bool> IsTokenValidAsync(string token);
    }
}
