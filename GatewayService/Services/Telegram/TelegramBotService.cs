using GatewayService.Configuration;
using GatewayService.Handlers;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace GatewayService.Services.Telegram
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ILogger<TelegramBotService> _logger;
        private readonly ICommandHandler _commandHandler;
        private readonly IMessageSender _messageSender;

        public TelegramBotService(IOptions<TelegramSettings> telegramSettings, ILogger<TelegramBotService> logger, ICommandHandler commandHandler, IMessageSender messageSender)
        {
            _logger = logger;
            _commandHandler = commandHandler;
            _messageSender = messageSender;
        }
        public async Task HandleUpdateAsync(Update update)
        {
            if (update == null || update.Message == null || update.Message.Text == null)
                return;
        
            var message = update.Message;
            var chatId = message.Chat.Id;
            var messageText = message.Text;

            _logger.LogInformation("Received message from ChatId: {ChatId}, Text: {Text}", chatId, messageText);

            if (messageText.StartsWith("/start"))
                await _commandHandler.HandleStartCommand(chatId);
            else if (messageText.StartsWith("/help"))
                await _commandHandler.HandleHelpCommand(chatId);
            else if (messageText.StartsWith("/register"))
                await _commandHandler.HandleRegisterCommand(chatId, messageText);
            else if (messageText.StartsWith("/login"))
                await _commandHandler.HandleLoginCommand(chatId, messageText);
            else if (messageText.StartsWith("/myprofile"))
                await _commandHandler.HandleMyProfileCommand(chatId);
            else if (messageText.StartsWith("/update_profile"))
                await _commandHandler.HandleUpdateProfileCommand(chatId, messageText);
            else
                await _messageSender.SendMessageAsync(chatId, "Неизвестная команда. Используйте /help");
        }
    }
}
