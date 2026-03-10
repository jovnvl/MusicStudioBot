using GatewayService.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace GatewayService.Services
{
    public class MessageSenderService : IMessageSender
    {
        private readonly TelegramBotClient _botClient;

        public MessageSenderService(IOptions<TelegramSettings> settings)
        {
            _botClient = new TelegramBotClient(settings.Value.BotToken);
        }

        public async Task SendMessageAsync(long chatId, string text)
        {
            await _botClient.SendMessage(chatId: chatId, text: text);
        }
    }
}
