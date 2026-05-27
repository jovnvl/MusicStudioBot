using GatewayService.Configuration;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using GatewayService.Services.Telegram;
using Microsoft.Extensions.Options;

namespace GatewayService.Handlers
{
    public class SystemCommandHandler : CommandHandler
    {
        public SystemCommandHandler(
            IHttpClientFactory httpClientFactory, 
            ILogger<CommandHandler> logger, 
            IUserSessionService sessionService, 
            IMessageSender messageSender, 
            IRabbitMQPublisher rabbitMQPublisher) : base(httpClientFactory, logger, sessionService, messageSender, rabbitMQPublisher)
        {
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
/bookings - Получить информацию о бронированиях";
            await _messageSender.SendMessageAsync(chatId, helpMessage);
        }
    }
}
