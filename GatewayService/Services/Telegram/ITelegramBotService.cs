using Telegram.Bot.Types;

namespace GatewayService.Services.Telegram
{
    public interface ITelegramBotService
    {
        Task HandleUpdateAsync(Update update);
        Task SendMessageAsync(long chatId, string text);
    }
}