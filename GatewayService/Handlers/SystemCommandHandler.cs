using GatewayService.Configuration;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using GatewayService.Services.Telegram;
using Microsoft.Extensions.Options;

namespace GatewayService.Handlers
{
    public class SystemCommandHandler : CommandHandler
    {
        private readonly ServicesSettings _servicesSettings;
        public SystemCommandHandler(
            IHttpClientFactory httpClientFactory, 
            ILogger<CommandHandler> logger, 
            IUserSessionService sessionService, 
            IMessageSender messageSender, 
            IRabbitMQPublisher rabbitMQPublisher,
            IOptions<ServicesSettings> servicesSettings) : base(httpClientFactory, logger, sessionService, messageSender, rabbitMQPublisher)
        {
            _servicesSettings = servicesSettings.Value;
        }

        public async Task HandleStartCommand(long chatId)
        {
            var (accessToken, _) = await _sessionService.GetTokensAsync(chatId);
            var isAuthenticated = !string.IsNullOrEmpty(accessToken) &&
                                  await _sessionService.IsTokenValidAsync(accessToken);

            string? userRole = null;

            if (isAuthenticated)
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(accessToken);
                userRole = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            }

            string welcomeMessage = isAuthenticated
                ? "🎵 Music Studio Bot\n\nВыберите действие из меню:"
                : "🎵 Приветствуем в Music Studio Bot!\n\nЭтот бот поможет вам забронировать комнату для репетиций.\n\n⚠️ Для начала работы необходимо зарегистрироваться.";

            var keyboard = KeyboardHelper.GetMainMenu(isAuthenticated, userRole);
            await _messageSender.SendMessageAsync(chatId, welcomeMessage, replyMarkup: keyboard);
        }

        public async Task HandleHelpCommand(long chatId)
        {
            string helpMessage = @"Доступные команды:
/start - Начать работу
/help - Список всех команд
/myprofile - Получить данные профиля
/rooms - Получить информацию о комнатах
/bookings - Получить информацию о бронированиях
/logout - Выйти из системы";
            await _messageSender.SendMessageAsync(chatId, helpMessage);
        }

        public async Task HandleLogoutCommand(long chatId)
        {
            var (_, refreshToken) = await _sessionService.GetTokensAsync(chatId);

            if (string.IsNullOrEmpty(refreshToken))
            {
                await _messageSender.SendMessageAsync(chatId, "Вы не авторизованы.");
                return;
            }

            try
            {
                await SendRequestAsync(HttpMethod.Post,
                    $"{_servicesSettings.IdentityServiceUrl}/api/auth/revoke",
                    chatId,
                    new { RefreshToken = refreshToken });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revoke token for {ChatId}, proceeding with local logout", chatId);
            }

            await _sessionService.RemoveTokenAsync(chatId);
            await _messageSender.SendMessageAsync(chatId, "✅ Вы вышли из системы.");
            await HandleStartCommand(chatId);
        }
    }
}
