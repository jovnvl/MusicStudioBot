using GatewayService.Configuration;
using GatewayService.Handlers;
using GatewayService.Middleware;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace GatewayService.Services.Telegram
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMessageSender _messageSender;

        public TelegramBotService(IOptions<TelegramSettings> telegramSettings, ILogger<TelegramBotService> logger, IMessageSender messageSender, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _messageSender = messageSender;
            _scopeFactory = scopeFactory;
        }
        public async Task HandleUpdateAsync(Update update)
        {
            if (update?.Message?.Text == null)
                return;

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;
            var chatUsername = update.Message.Chat.Username ?? chatId.ToString();
            
            _logger.LogInformation("Received message from ChatId: {ChatId}, Text: {Text}", chatId, messageText);

            using var scope = _scopeFactory.CreateScope();

            // Исключаем команды, которые не требуют авторизации
            var publicCommands = new[] { "/start", "/help", "/register" };
            var isPublicCommand = publicCommands.Any(cmd => messageText.StartsWith(cmd));

            // Проверяем авторизацию для всех остальных команд
            if (!isPublicCommand)
            {
                var authMiddleware = scope.ServiceProvider.GetRequiredService<TelegramAuthMiddleware>();
                var isAuthenticated = await authMiddleware.EnsureAuthenticatedAsync(chatId);

                if (!isAuthenticated)
                {
                    await _messageSender.SendMessageAsync(chatId,
                        "Вы не зарегистрированы. Используйте /register для создания аккаунта.");
                    return;
                }
            }

            try
            {
                if (messageText.StartsWith("/start"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<SystemCommandHandler>();
                    await handler.HandleStartCommand(chatId);
                }
                else if (messageText.StartsWith("/help"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<SystemCommandHandler>();
                    await handler.HandleHelpCommand(chatId);
                }
                else if (messageText.StartsWith("/register"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await handler.HandleRegisterCommand(chatId, chatUsername, messageText);
                }
                else if (messageText.StartsWith("/myprofile"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await handler.HandleMyProfileCommand(chatId);
                }
                else if (messageText.StartsWith("/update_profile"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await handler.HandleUpdateProfileCommand(chatId, messageText);
                }
                else if (messageText.StartsWith("/users"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await handler.HandleGetUsersCommand(chatId);
                }
                else if (messageText.StartsWith("/change_role"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await handler.HandleChangeUserRoleCommand(chatId, messageText);
                }
                else if (messageText.StartsWith("/rooms"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                    await handler.HandleGetRoomsCommand(chatId);
                }
                else if (messageText.StartsWith("/create_room_category"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                    await handler.HandleCreateRoomCategoryCommand(chatId, messageText);
                }
                else if (messageText.StartsWith("/create_room"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                    await handler.HandleCreateRoomCommand(chatId, messageText);
                }
                else if (messageText.StartsWith("/update_room_status"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                    await handler.HandleUpdateRoomCommand(chatId, messageText);
                }
                else if (messageText.StartsWith("/get_room"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<RoomCommandHandler>();
                    await handler.HandleGetRoomCommand(chatId, messageText);
                }
                else if (messageText.StartsWith("/create_booking"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                    await handler.HandleCreateBookingCommand(chatId, messageText);
                }
                else if (messageText.StartsWith("/get_bookings"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<BookingCommandHandler>();
                    await handler.HandleGetBookingsCommand(chatId);
                }
                else
                    await _messageSender.SendMessageAsync(chatId, "Неизвестная команда. Используйте /help");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error handling update");
            }
        }
    }
}
