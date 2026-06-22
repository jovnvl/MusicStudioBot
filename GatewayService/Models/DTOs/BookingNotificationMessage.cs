using GatewayService.Models.Enums;

/*
Что уже есть:

В IdentityService у пользователя есть поле TelegramId (это и есть chatId для личных сообщений от бота).
Есть endpoint получения пользователя по Id:
GET /api/auth/user/{id}.
Есть endpoint получения пользователей по роли:
GET /api/auth/user/role/{userRole}.
В GatewayService.Models.DTOs.UserResponse уже приходит TelegramId.

Поэтому для уведомлений достаточно:

Получить владельца бронирования через /api/auth/user/{userId}.
Получить всех модераторов через /api/auth/user/role/1.
Взять из ответа TelegramId.
Отправить уведомление.
 */
namespace GatewayService.Models.DTOs
{
    public class BookingNotificationMessage
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }

        public int RoomId { get; set; }

        public BookingStatus Status { get; set; }

        public DateTime TimeBegin { get; set; }
        public DateTime TimeEnd { get; set; }

        public string? Description { get; set; }

        public string EventType { get; set; } = string.Empty;
    }
}
