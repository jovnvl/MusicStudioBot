using GatewayService.Configuration;
using GatewayService.Models.DTOs;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using static System.Net.Mime.MediaTypeNames;

namespace GatewayService.Services.Notifications
{
    public class BookingNotificationHandler
    {
        private readonly IMessageSender _messageSender;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BookingNotificationHandler> _logger;
        private readonly ServicesSettings _servicesSettings;

        public BookingNotificationHandler(
            IMessageSender messageSender,
            IHttpClientFactory httpClientFactory,
            ILogger<BookingNotificationHandler> logger,
            IOptions<ServicesSettings> servicesSettings)
        {
            _messageSender = messageSender;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _servicesSettings = servicesSettings.Value;
        }

        public async Task HandleAsync(BookingNotificationDto message)
        {
            try
            {
                var text = message.EventType switch
                {
                    "created" =>
                        $"""
                🎵 Новое бронирование

                Начало: {message.BookingDto.TimeBegin:dd.MM.yyyy HH:mm}
                Окончание: {message.BookingDto.TimeEnd:dd.MM.yyyy HH:mm}

                {message.BookingDto.Description}
                """,

                    "updated" =>
                        $"""
                ✏️ Бронирование изменено

                Начало: {message.BookingDto.TimeBegin:dd.MM.yyyy HH:mm}
                Окончание: {message.BookingDto.TimeEnd:dd.MM.yyyy HH:mm}

                {message.BookingDto.Description}
                """,

                    "approved" =>
                        $"""
                ✅ Бронирование подтверждено

                Начало: {message.BookingDto.TimeBegin:dd.MM.yyyy HH:mm}
                Окончание: {message.BookingDto.TimeEnd:dd.MM.yyyy HH:mm}
                """,

                    "rejected" =>
                        $"""
                ❌ Бронирование отклонено

                Начало: {message.BookingDto.TimeBegin:dd.MM.yyyy HH:mm}
                Окончание: {message.BookingDto.TimeEnd:dd.MM.yyyy HH:mm}
                """,

                    "deleted" =>
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

                if (message.EventType is "created" or "updated" or "deleted")
                {
                    await NotifyModeratorsAsync(text);
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
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync(
                $"{_servicesSettings.IdentityServiceUrl}/api/auth/user/{userId}");

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

            await _messageSender.SendMessageAsync(
                user.TelegramId,
                text);
        }

        private async Task NotifyModeratorsAsync(string text)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync(
                $"{_servicesSettings.IdentityServiceUrl}/api/auth/user/role/1");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to load moderators list");

                return;
            }

            var moderators =
                await response.Content.ReadFromJsonAsync<List<UserResponse>>();

            if (moderators == null)
                return;

            foreach (var moderator in moderators)
            {
                try
                {
                    await _messageSender.SendMessageAsync(
                        moderator.TelegramId,
                        text);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed notify moderator {ModeratorId}",
                        moderator.Id);
                }
            }
        }
    }
}
