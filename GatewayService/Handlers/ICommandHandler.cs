namespace GatewayService.Handlers
{
    public interface ICommandHandler
    {
            Task HandleStartCommand(long chatId);
            Task HandleHelpCommand(long chatId);
            Task HandleRegisterCommand(long chatId, string messageText);
            Task HandleLoginCommand(long chatId, string messageText);
            Task HandleMyProfileCommand(long chatId);
    }
}
