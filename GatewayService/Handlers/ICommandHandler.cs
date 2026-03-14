namespace GatewayService.Handlers
{
    public interface ICommandHandler
    {
        Task HandleStartCommand(long chatId);
        Task HandleHelpCommand(long chatId);
        Task HandleRegisterCommand(long chatId, string messageText);
        Task HandleLoginCommand(long chatId, string messageText);
        Task HandleMyProfileCommand(long chatId);
        Task HandleUpdateProfileCommand(long chatId, string messageText);
        Task HandleGetRoomsCommand(long chatId);
        Task HandleGetRoomCommand(long chatId, string messageText);
        Task HandleCreateRoomCommand (long chatId, string messageText);
        Task HandleUpdateRoomCommand (long chatId, string messageText);
        Task HandleCreateRoomCategoryCommand(long chatId, string messageText);
        Task HandleGetUsersCommand(long chatId);
        Task HandleChangeUserRoleCommand(long chatId, string messageText);
    }
}
