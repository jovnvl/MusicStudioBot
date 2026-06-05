using Telegram.Bot.Types.ReplyMarkups;

namespace GatewayService.Services
{
    public interface IMessageSender
    {
        Task SendMessageAsync(long chatId, string text, ReplyMarkup? replyMarkup = null);
    }
}