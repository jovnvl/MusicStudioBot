using GatewayService.Configuration;
using GatewayService.DTO;
using GatewayService.Models.DTOs;
using GatewayService.Models.Enums;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GatewayService.Handlers
{
    public class BookingCommandHandler : CommandHandler
    {
        private readonly ServicesSettings _servicesSettings;
        private const int CreateBookingCommandPartsCount = 3;

        public BookingCommandHandler(IHttpClientFactory httpClientFactory, ILogger<CommandHandler> logger, IUserSessionService sessionService, IMessageSender messageSender, IRabbitMQPublisher rabbitMQPublisher, IOptions<ServicesSettings> servicesSettings) : base(httpClientFactory, logger, sessionService, messageSender, rabbitMQPublisher)
        {
            _servicesSettings = servicesSettings.Value;
        }

        public async Task HandleCreateBookingCommand(long chatId, string messageText)
        {
            if (!await IsPermitted(chatId, UserRole.Moderator))
                return;
            string[] createBookingCommand = messageText.Split('|');
            if (createBookingCommand.Length < CreateBookingCommandPartsCount)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /create_booking userId | roomId | timeBegin | timeEnd");
                return;
            }
            string userIdString = createBookingCommand[0].Substring(createBookingCommand[0].IndexOf(' ') + 1).Trim();
            string roomIdString = createBookingCommand[1].Trim();
            string timeBeginString = createBookingCommand[2].Trim();
            string timeEndString = createBookingCommand[3].Trim();

            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный userId.");
                return;
            }
            if (!int.TryParse(roomIdString, out int roomId))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный roomId.");
                return;
            }
            if (!DateTime.TryParse(timeBeginString, out DateTime timeBegin))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат времени начала!\nИспользуйте: dd.MM.yyyy HH:mm:ss");
                return;
            }
            if (!DateTime.TryParse(timeEndString, out DateTime timeEnd))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат времени окончания!\nИспользуйте: dd.MM.yyyy HH:mm:ss");
                return;
            }
            if (timeBegin > timeEnd)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Время начала больше времени окончания!");
                return;
            }

            var createBookingRequest = new CreateBookingRequest
            {
                Description = "Бронирование комнаты",
                UserId = userId,
                RoomId = roomId,
                Status = BookingStatus.NotConfirmed,
                TimeBegin = timeBegin.ToUniversalTime(),
                TimeEnd = timeEnd.ToUniversalTime(),
            };

            try
            {
                var response = await SendRequestAsync(HttpMethod.Post, $"{_servicesSettings.BookingServiceUrl}/api/booking", chatId, createBookingRequest);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var bookingResponse = JsonSerializer.Deserialize<BookingResponse>(body);
                    if (bookingResponse == null)
                    {
                        _logger.LogError("Failed to deserialize BookingResponse");
                        return;
                    }
                    _logger.LogInformation("Added new booking.");

                    await _messageSender.SendMessageAsync(chatId, "Добавлена запись о бронировании");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to BookingService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
            }
        }

        public async Task HandleGetBookingsCommand(long chatId)
        {
            if (!await IsPermitted(chatId, UserRole.Moderator))
                return;
            try
            {
                var response = await SendRequestAsync(HttpMethod.Get, $"{_servicesSettings.BookingServiceUrl}/api/booking", chatId);
                var body = await response.Content.ReadAsStringAsync();
                var bookings = JsonSerializer.Deserialize<List<BookingResponse>>(body);
                if (bookings == null || bookings.Count == 0)
                {
                    _logger.LogError("Failed to deserialize BookingResponse");
                    await LogToServiceAsync("Error", "book-response-fail", "Failed to deserialize UserResponse");
                    await _messageSender.SendMessageAsync(chatId, "Пользователей пока нет");
                    return;
                }
                _logger.LogInformation("Successful receipt of bookings information.");
                await LogToServiceAsync("Information", "get-bookings", "Successful receipt of bookings information.");
                var message = "Список бронирований:\n\n";
                foreach (var booking in bookings)
                {

                    message += $"* Id пользователя {booking.UserId}, id комнаты:{booking.RoomId}, описание: {booking.Description})\n";
                    message += $"   └ {booking.TimeBegin} - {booking.TimeEnd}\n\n";
                }
                await _messageSender.SendMessageAsync(chatId, message);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to BookingService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
            }
        }
    }
}
