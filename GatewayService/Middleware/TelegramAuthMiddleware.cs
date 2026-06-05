using GatewayService.Configuration;
using GatewayService.Models.DTOs;
using GatewayService.Services;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace GatewayService.Middleware
{
    public class TelegramAuthMiddleware
    {
        private readonly IUserSessionService _sessionService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TelegramAuthMiddleware> _logger;
        private readonly ServicesSettings _servicesSettings;

        public TelegramAuthMiddleware(
            IUserSessionService sessionService,
            IHttpClientFactory httpClientFactory,
            ILogger<TelegramAuthMiddleware> logger,
            IOptions<ServicesSettings> servicesSettings)
        {
            _sessionService = sessionService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _servicesSettings = servicesSettings.Value;
        }

        public async Task<bool> EnsureAuthenticatedAsync(long telegramId)
        {
            // 1. Проверяем существующую сессию
            var (accessToken, refreshToken) = await _sessionService.GetTokensAsync(telegramId);

            // 2. Если есть access token и он валиден - всё ок
            if (!string.IsNullOrEmpty(accessToken) && await _sessionService.IsTokenValidAsync(accessToken))
            {
                _logger.LogDebug("User {TelegramId} has valid session", telegramId);
                return true;
            }

            // 3. Если access token истек, пробуем refresh
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var refreshed = await TryRefreshTokenAsync(telegramId, refreshToken);
                if (refreshed)
                {
                    _logger.LogInformation("Refreshed token for user {TelegramId}", telegramId);
                    return true;
                }
            }

            // 4. Если ничего не помогло - пробуем автологин
            var loggedIn = await TryAutoLoginAsync(telegramId);
            if (loggedIn)
            {
                _logger.LogInformation("Auto-login successful for user {TelegramId}", telegramId);
                return true;
            }

            // 5. Пользователь не зарегистрирован
            _logger.LogWarning("User {TelegramId} is not registered", telegramId);
            return false;
        }

        private async Task<bool> TryRefreshTokenAsync(long telegramId, string refreshToken)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var request = new { RefreshToken = refreshToken };
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync(
                    $"{_servicesSettings.IdentityServiceUrl}/api/auth/refresh",
                    content
                );

                if (!response.IsSuccessStatusCode)
                {
                    await _sessionService.RemoveTokenAsync(telegramId);
                    return false;
                }

                var body = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(body);

                if (authResponse != null)
                {
                    await _sessionService.SaveTokenAsync(
                        telegramId,
                        authResponse.Token,
                        authResponse.RefreshToken
                    );
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token for {TelegramId}", telegramId);
                return false;
            }
        }

        private async Task<bool> TryAutoLoginAsync(long telegramId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var request = new TelegramLoginRequest { TelegramId = telegramId };
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync(
                    $"{_servicesSettings.IdentityServiceUrl}/api/auth/telegram-login",
                    content
                );

                if (!response.IsSuccessStatusCode)
                    return false;

                var body = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(body);

                if (authResponse != null)
                {
                    await _sessionService.SaveTokenAsync(
                        telegramId,
                        authResponse.Token,
                        authResponse.RefreshToken
                    );
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-login for {TelegramId}", telegramId);
                return false;
            }
        }
    }
}