using GatewayService.Configuration;
using GatewayService.Handlers;
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
            if (update == null || update.Message == null || update.Message.Text == null)
                return;

            var message = update.Message;
            var chatId = message.Chat.Id;
            var chatUsername = message.Chat.Username ?? chatId.ToString();
            var messageText = message.Text;

            _logger.LogInformation("Received message from ChatId: {ChatId}, Text: {Text}", chatId, messageText);

            using var scope = _scopeFactory.CreateScope();
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
                else if (messageText.StartsWith("/login"))
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IdentityCommandHandler>();
                    await handler.HandleLoginCommand(chatId, messageText);
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
