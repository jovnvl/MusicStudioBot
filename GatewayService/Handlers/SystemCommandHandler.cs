using GatewayService.Configuration;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using Microsoft.Extensions.Options;

namespace GatewayService.Handlers
{
    public class SystemCommandHandler : CommandHandler
    {
        public SystemCommandHandler(IHttpClientFactory httpClientFactory, ILogger<CommandHandler> logger, IUserSessionService sessionService, IMessageSender messageSender, IRabbitMQPublisher rabbitMQPublisher) : base(httpClientFactory, logger, sessionService, messageSender, rabbitMQPublisher)
        {
        }

        public async Task HandleStartCommand(long chatId)
        {
            string welcomeMessage = @"
Приветствуем в Music Studio Bot! 🎵
   
Этот бот поможет вам забронировать комнату для репетиций
            ";
            await _messageSender.SendMessageAsync(chatId, welcomeMessage);
        }

        public async Task HandleHelpCommand(long chatId)
        {
            string helpMessage = @"Доступные команды:
/start - Начать работу
/register - Регистрация (формат: /register username password firstname lastname)
/login - Вход (формат: /login password)
/help - Список всех команд
/myprofile - Получить данные профиля
/update_profile - Изменить профиль (формат: /update_profile [username] [firstname] [lastname])
/users - Получить список пользователей
/change_role - Изменить роль пользователя (формат: /change_role id role)
/rooms - Получить информацию о комнатах
/create_room - Создать комнату (формат: /create_room name | category_id | description)
/create_room_category - Создать категорию (формат: /create_room_category name | description)
/get_room - Получить комнату (формат: /get_room id)
/update_room_status - Обновить статус (формат: /update_room_status id status)
/create_booking - Создать бронирование (формат: /create_booking userId | roomId | timeBegin | timeEnd)
/get_bookings - Получить информацию о бронированиях";
            await _messageSender.SendMessageAsync(chatId, helpMessage);
        }
    }
}
