namespace GatewayService.Services
{
    public interface IMessageSender
    {
        Task SendMessageAsync(long chatId, string text);
    }
}
