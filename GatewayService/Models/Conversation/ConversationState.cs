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

        // Создание/изменение бронирования
        AwaitingRoomSelection,
        AwaitingBookingDate,
        AwaitingBookingStartTime,
        AwaitingBookingEndTime,
        AwaitingBookingSelection,
        AwaitingBookingStatus,

        //Изменение статуса комнаты
        AwaitingRoomStatusSelection,
        AwaitingRoomStatusConfirmation,
    }
}
