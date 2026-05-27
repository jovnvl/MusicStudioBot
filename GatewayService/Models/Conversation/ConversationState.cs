namespace GatewayService.Models.Conversation
{
    public enum ConversationState
    {
        None,

        // Регистрация
        AwaitingRegistrationPassword,
        AwaitingRegistrationFirstName,
        AwaitingRegistrationLastName,

        // Обновление профиля
        AwaitingUpdateProfileField,

        // Создание бронирования
        AwaitingRoomSelection,
        AwaitingBookingDate,
        AwaitingBookingStartTime,
        AwaitingBookingEndTime,
        AwaitingBookingSelection,
        AwaitingBookingStatus
    }
}
