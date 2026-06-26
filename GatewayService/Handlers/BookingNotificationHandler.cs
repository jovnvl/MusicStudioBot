using GatewayService.Configuration;
using GatewayService.Handlers;
using GatewayService.Models.DTOs;
using GatewayService.Models.Enums;
using GatewayService.Services.RabbitMQ;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Telegram.Bot.Types;

namespace GatewayService.Services.Notifications
{
    public class BookingNotificationHandler : CommandHandler
    {
        private readonly ServicesSettings _servicesSettings;

        public BookingNotificationHandler(
          IHttpClientFactory httpClientFactory,
            ILogger<CommandHandler> logger,
            IUserSessionService sessionService,
            IMessageSender messageSender,
            IRabbitMQPublisher rabbitMQPublisher,
            IOptions<ServicesSettings> servicesSettings) : base(httpClientFactory, logger, sessionService, messageSender, rabbitMQPublisher)
        {
            _servicesSettings = servicesSettings.Value;
        }


        public async Task HandleAsync(BookingNotificationDto message)
        {
            _logger.LogInformation(
                "Received booking notification. Event={EventType}, Booking={BookingId}",
                message.EventType,
                message.BookingDto.Id);
            try
            {
                var text = message.EventType switch
                {
                    "create-booking" =>
                        $"""
                🎵 Новое бронирование

                Начало: {message.BookingDto.TimeBegin:dd.MM.yyyy HH:mm}
                Окончание: {message.BookingDto.TimeEnd:dd.MM.yyyy HH:mm}

                {message.BookingDto.Description}
                """,

                    "update-booking" =>
                        $"""
                ✏️ Бронирование изменено

                Начало: {message.BookingDto.TimeBegin:dd.MM.yyyy HH:mm}
                Окончание: {message.BookingDto.TimeEnd:dd.MM.yyyy HH:mm}

                {message.BookingDto.Description}
                """,

                    "approve-booking" =>
                        $"""
                ✅ Бронирование подтверждено

                Начало: {message.BookingDto.TimeBegin:dd.MM.yyyy HH:mm}
                Окончание: {message.BookingDto.TimeEnd:dd.MM.yyyy HH:mm}
                """,

                    "reject-booking" =>
                        $"""
                ❌ Бронирование отклонено

                Начало: {message.BookingDto.TimeBegin:dd.MM.yyyy HH:mm}
                Окончание: {message.BookingDto.TimeEnd:dd.MM.yyyy HH:mm}
                """,

                    "delete-booking" =>
                        $"""
                🗑 Бронирование отменено

                Начало: {message.BookingDto.TimeBegin:dd.MM.yyyy HH:mm}
                Окончание: {message.BookingDto.TimeEnd:dd.MM.yyyy HH:mm}
                """,

                    _ =>
                        $"""
                ℹ️ Изменение бронирования

                Начало: {message.BookingDto.TimeBegin:dd.MM.yyyy HH:mm}
                Окончание: {message.BookingDto.TimeEnd:dd.MM.yyyy HH:mm}
                """
                };

                await NotifyOwnerAsync(message.BookingDto.UserId, text);

                if (message.EventType is "create-booking" or "update-booking" or "approve-booking" or "delete-booking" or "reject-booking")
                {
                    var moderators = await UsersWithRoleAsync(UserRole.Moderator);
                    moderators?.RemoveAll(u => u.Id == message.BookingDto.UserId);
                    await NotifyUsersAsync(moderators, text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing booking notification {BookingId}",
                    message.BookingDto.Id);
            }
        }

        private async Task NotifyOwnerAsync(Guid userId, string text)
        {
            var response = await SendRequestAsync(HttpMethod.Get,
                    $"{_servicesSettings.IdentityServiceUrl}/api/auth/user/{userId}", 0);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "User {UserId} not found for notification",
                    userId);

                return;
            }

            var user = await response.Content
                .ReadFromJsonAsync<UserResponse>();

            if (user == null)
                return;

            _logger.LogInformation(
                    "Sending notification to user {UserId}, TelegramId={TelegramId}",
                    userId,
                    user.TelegramId);
            await _messageSender.SendMessageAsync(
                user.TelegramId,
                text);
        }

        private async Task<List<UserResponse>?> UsersWithRoleAsync(UserRole userRole)
        {
            List<UserResponse>? users = new List<UserResponse>();
            var response = await SendRequestAsync(HttpMethod.Get,
                                $"{_servicesSettings.IdentityServiceUrl}/api/auth/user/role/{(int)userRole}", 0);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    $"Failed to load list users with role {userRole}");

            }
            else
                users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
            return users;
        }


        private async Task NotifyUsersAsync( List<UserResponse>? users, string text)
        {
            if (users == null)
                return;
            foreach (var user in users)
            {
                try
                {
                    _logger.LogInformation(
                            "Sending notification to user {UserId}, TelegramId={TelegramId}",
                            user.Id,
                            user.TelegramId);
                    await _messageSender.SendMessageAsync(
                        user.TelegramId,
                        text);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed notify user {ModeratorId}",
                        user.Id);
                }
            }
        }
    }
}
