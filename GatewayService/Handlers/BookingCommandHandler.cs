using GatewayService.Configuration;
using GatewayService.DTO;
using GatewayService.Models.Conversation;
using GatewayService.Models.DTOs;
using GatewayService.Models.Enums;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using GatewayService.Services.Telegram;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace GatewayService.Handlers
{
    public class BookingCommandHandler : CommandHandler
    {
        private readonly ServicesSettings _servicesSettings;
        private readonly IConversationStateService _conversationService;

        public BookingCommandHandler(
            IHttpClientFactory httpClientFactory, 
            ILogger<CommandHandler> logger, 
            IUserSessionService sessionService, 
            IMessageSender messageSender, 
            IRabbitMQPublisher rabbitMQPublisher, 
            IOptions<ServicesSettings> servicesSettings,
            IConversationStateService conversationService) : base(httpClientFactory, logger, sessionService, messageSender, rabbitMQPublisher)
        {
            _servicesSettings = servicesSettings.Value;
            _conversationService = conversationService;
        }

        protected string MapBookingStatusToEmoji(BookingStatus? status)
        {
            return status switch
            {
                BookingStatus.NotConfirmed => "🟡",
                BookingStatus.Canceled => "🔴",
                BookingStatus.Booked => "🟢",
                BookingStatus.Completed => "⚪",
                _ => "❓"
            };
        }

        private string ComposeBookingString(
            BookingResponse booking,
            Dictionary<Guid, string> userNames,
            Dictionary<int, string> roomNames)
        {
                var userName = userNames.TryGetValue(booking.UserId, out var uName) ? uName : "Неизвестный";
                var roomName = roomNames.TryGetValue(booking.RoomId, out var rName) ? rName : "Неизвестно";
                var emoji = MapBookingStatusToEmoji(booking.Status);
                var message = $"{emoji} {booking.Period?.TimeBegin?.ToLocalTime().ToString("dd.MM.yyyy HH:mm")} - {booking.Period?.TimeEnd?.ToLocalTime().ToString("dd.MM.yyyy HH:mm")}\n";
                message += $"   ├ {userName}\n";
                message += $"   ├ {roomName}\n";
                message += $"   ├ {booking.Status.ToString()}\n";
                message += $"   └ комментарий: {booking.Description}\n\n";


            return message;
        }

        private async Task<Dictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds, long chatId)
        {
            var userNames = new Dictionary<Guid, string>();

            foreach (var userId in userIds)
            {
                var userResponse = await SendRequestAsync(HttpMethod.Get,
                    $"{_servicesSettings.IdentityServiceUrl}/api/auth/user/{userId}", chatId);

                if (userResponse.IsSuccessStatusCode)
                {
                    var userBody = await userResponse.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<UserResponse>(userBody);
                    if (user != null)
                        userNames[userId] = $"{user.FirstName} {user.LastName} ({user.Username})";
                }
            }
            return userNames;
        }

        private async Task<Dictionary<int, string>> GetRoomNamesAsync(IEnumerable<int> roomIds, long chatId)
        {
            var roomNames = new Dictionary<int, string>();

            foreach (var roomId in roomIds)
            {
                var roomResponse = await SendRequestAsync(HttpMethod.Get,
                    $"{_servicesSettings.RoomServiceUrl}/api/rooms/{roomId}", chatId);

                if (roomResponse.IsSuccessStatusCode)
                {
                    var roomBody = await roomResponse.Content.ReadAsStringAsync();
                    var room = JsonSerializer.Deserialize<RoomResponse>(roomBody);
                    if (room != null)
                        roomNames[roomId] = $"{room.Name}";
                }
            }
            return roomNames;
        }

        public async Task HandleUpdateBookingInput(long chatId, UserConversationData conversation)
        {
            if (!await IsPermitted(chatId, UserRole.Moderator))
            {
                conversation.Clear();
                return;
            }

            try
            {
                var response = await SendRequestAsync(HttpMethod.Get, $"{_servicesSettings.BookingServiceUrl}/api/booking", chatId);

                if (!response.IsSuccessStatusCode)
                {
                    await _messageSender.SendMessageAsync(chatId, "Ошибка получения списка бронирований.");
                    conversation.Clear();
                    return;
                }

                var body = await response.Content.ReadAsStringAsync();
                var bookings = JsonSerializer.Deserialize<List<BookingResponse>>(body);

                if (bookings == null || bookings.Count == 0)
                {
                    await _messageSender.SendMessageAsync(chatId, "Бронирований пока нет");
                    return;
                }

                bookings = bookings.OrderBy(b => b.Period?.TimeBegin).ToList();

                var userNames = await GetUserNamesAsync(bookings.Select(b => b.UserId).Distinct(), chatId);
                var roomNames = await GetRoomNamesAsync(bookings.Select(b => b.RoomId).Distinct(), chatId);

                conversation.State = ConversationState.AwaitingBookingSelection;

                var keyboard = KeyboardHelper.GetBookingSelectionKeyboard(bookings, userNames, roomNames);
                await _messageSender.SendMessageAsync(chatId,
                    "Смена статуса бронирования\n\nВыберите бронирование:",
                    replyMarkup: keyboard);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to BookingService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
            }
        }

        public async Task HandleBookingSelectionCallback(long chatId, string callbackData)
        {
            var conversation = _conversationService.GetOrCreate(chatId);

            if (conversation.State != ConversationState.AwaitingBookingSelection)
            {
                conversation.Clear();
                await _messageSender.SendMessageAsync(chatId, "Ошибка состояния. Начните заново.");
                return;
            }

            // Извлекаем bookingId из callback (формат: "booking_5")
            var bookingIdString = callbackData.Replace("booking_", "");
            if (!Guid.TryParse(bookingIdString, out Guid bookingId))
            {
                await _messageSender.SendMessageAsync(chatId, "Ошибка выбора бронирования.");
                conversation.Clear();
                return;
            }

            conversation.SetValue("bookingId", bookingId);

            // Переходим к выбору статуса
            conversation.State = ConversationState.AwaitingBookingStatus;
            await _messageSender.SendMessageAsync(chatId,
                "Отлично! Теперь выберите статус",
                replyMarkup: KeyboardHelper.GetBookingStatusSelectionKeyboard());
        }

        public async Task HandleBookingStatusSelectionCallback(long chatId, string callbackData)
        {
            var conversation = _conversationService.GetOrCreate(chatId);

            if (conversation.State != ConversationState.AwaitingBookingStatus)
            {
                conversation.Clear();
                await _messageSender.SendMessageAsync(chatId, "Ошибка состояния. Начните заново.");
                return;
            }

            var statusString = callbackData.Replace("status_", "");
            if (!Enum.TryParse<BookingStatus>(statusString, out var bookingStatus))
            {
                await _messageSender.SendMessageAsync(chatId, "Ошибка выбора статуса.");
                conversation.Clear();
                return;
            }

            var bookingId = conversation.GetValue<Guid>("bookingId");

            try
            {
                // Получаем текущее бронирование
                var getResponse = await SendRequestAsync(HttpMethod.Get,
                    $"{_servicesSettings.BookingServiceUrl}/api/booking/booking/{bookingId}", chatId);

                if (!getResponse.IsSuccessStatusCode)
                {
                    await _messageSender.SendMessageAsync(chatId, "Ошибка: бронирование не найдено.");
                    conversation.Clear();
                    return;
                }

                var body = await getResponse.Content.ReadAsStringAsync();
                var booking = JsonSerializer.Deserialize<BookingResponse>(body);

                if (booking == null)
                {
                    await _messageSender.SendMessageAsync(chatId, "Ошибка получения данных бронирования.");
                    conversation.Clear();
                    return;
                }

                // Отправляем полный объект с изменённым статусом
                var updateRequest = new BookingRequest
                {
                    Id = bookingId,
                    Description = booking.Description ?? string.Empty,
                    UserId = booking.UserId,
                    RoomId = booking.RoomId,
                    Status = bookingStatus,
                    TimeBegin = booking.Period?.TimeBegin,
                    TimeEnd = booking.Period?.TimeEnd
                };

                var putResponse = await SendRequestAsync(HttpMethod.Put,
                    $"{_servicesSettings.BookingServiceUrl}/api/booking",
                    chatId,
                    updateRequest);

                if (putResponse.IsSuccessStatusCode)
                {
                    await _messageSender.SendMessageAsync(chatId, $"✅ Статус бронирования обновлён на {bookingStatus}!");
                    await LogToServiceAsync("Information", "update-booking-status",
                        $"Booking {bookingId} status changed to {bookingStatus}");
                }
                else
                {
                    var error = await putResponse.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"❌ Ошибка: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update booking status");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером.");
            }
            finally
            {
                conversation.Clear();
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

                if (bookings == null)
                {
                    _logger.LogError("Failed to deserialize BookingResponse");
                    await LogToServiceAsync("Error", "book-response-fail", "Failed to deserialize BookingResponse");
                    await _messageSender.SendMessageAsync(chatId, "Ошибка получения данных. Попробуйте позже.");
                    return;
                }

                if (bookings.Count == 0)
                {
                    await _messageSender.SendMessageAsync(chatId, "Бронирований пока нет");
                    return;
                }

                bookings = bookings.OrderBy(b => b.Period?.TimeBegin).ToList();

                _logger.LogInformation("Successful receipt of bookings information.");
                await LogToServiceAsync("Information", "get-bookings", "Successful receipt of bookings information.");

                var userNames = await GetUserNamesAsync(bookings.Select(b => b.UserId).Distinct(), chatId);
                var roomNames = await GetRoomNamesAsync(bookings.Select(b => b.RoomId).Distinct(), chatId);

                var message = "Список бронирований:\n\n";
                foreach (var booking in bookings)
                {
                    message += ComposeBookingString(booking, userNames, roomNames);
                }

                await _messageSender.SendMessageAsync(chatId, message);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to BookingService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
            }
        }

        public async Task HandleGetMyBookingsCommand(long chatId)
        {
            if (!await IsPermitted(chatId, UserRole.Student))
                return;
            try
            {
                var token = await _sessionService.GetTokensAsync(chatId);
                var claims = new JwtSecurityTokenHandler().ReadJwtToken(token.accessToken).Claims;
                var tokenId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var response = await SendRequestAsync(HttpMethod.Get, $"{_servicesSettings.BookingServiceUrl}/api/booking/user/{tokenId}", chatId);
                var body = await response.Content.ReadAsStringAsync();
                var bookings = JsonSerializer.Deserialize<List<BookingResponse>>(body);

                if (bookings == null)
                {
                    _logger.LogError("Failed to deserialize BookingResponse");
                    await LogToServiceAsync("Error", "book-response-fail", "Failed to deserialize BookingResponse");
                    await _messageSender.SendMessageAsync(chatId, "Ошибка получения данных. Попробуйте позже.");
                    return;
                }

                if (bookings.Count == 0)
                {
                    await _messageSender.SendMessageAsync(chatId, "Актуальных бронирований нет.");
                    return;
                }

                bookings = bookings
                    .Where(b => b.Status != BookingStatus.Canceled && b.Status != BookingStatus.Completed)
                    .Where(b => b.Period?.TimeBegin >= DateTime.UtcNow.Date)
                    .OrderBy(b => b.Period?.TimeBegin)
                    .ToList();

                _logger.LogInformation("Successful receipt of bookings information.");
                await LogToServiceAsync("Information", "get-bookings", "Successful receipt of bookings information.");

                var userNames = await GetUserNamesAsync(bookings.Select(b => b.UserId).Distinct(), chatId);
                var roomNames = await GetRoomNamesAsync(bookings.Select(b => b.RoomId).Distinct(), chatId);

                var message = "Список бронирований:\n\n";
                foreach (var booking in bookings)
                {
                    message += ComposeBookingString(booking, userNames, roomNames);
                }

                await _messageSender.SendMessageAsync(chatId, message);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to BookingService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
            }
        }

        public async Task StartCreateBooking(long chatId, UserConversationData conversation)
        {
            if (!await IsPermitted(chatId, UserRole.Student))
            {
                conversation.Clear();
                return;
            }

            try
            {
                // Получаем список комнат
                var response = await SendRequestAsync(HttpMethod.Get,
                    $"{_servicesSettings.RoomServiceUrl}/api/rooms", chatId);

                if (!response.IsSuccessStatusCode)
                {
                    await _messageSender.SendMessageAsync(chatId, "Ошибка получения списка комнат.");
                    conversation.Clear();
                    return;
                }

                var body = await response.Content.ReadAsStringAsync();
                var rooms = JsonSerializer.Deserialize<List<RoomResponse>>(body);

                if (rooms == null || rooms.Count == 0)
                {
                    await _messageSender.SendMessageAsync(chatId, "Нет доступных комнат.");
                    conversation.Clear();
                    return;
                }

                // Переводим в состояние выбора комнаты
                conversation.State = ConversationState.AwaitingRoomSelection;

                var keyboard = KeyboardHelper.GetRoomSelectionKeyboard(rooms);
                await _messageSender.SendMessageAsync(chatId,
                    "📅 Создание бронирования\n\nВыберите комнату:",
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start booking creation");
                await _messageSender.SendMessageAsync(chatId, "Ошибка. Попробуйте позже.");
                conversation.Clear();
            }
        }

        public async Task HandleRoomSelectionCallback(long chatId, string callbackData)
        {
            var conversation = _conversationService.GetOrCreate(chatId);

            if (conversation.State != ConversationState.AwaitingRoomSelection)
            {
                conversation.Clear();
                await _messageSender.SendMessageAsync(chatId, "Ошибка состояния. Начните заново.");
                return;
            }

            // Извлекаем roomId из callback (формат: "room_5")
            var roomIdString = callbackData.Replace("room_", "");
            if (!int.TryParse(roomIdString, out int roomId))
            {
                await _messageSender.SendMessageAsync(chatId, "Ошибка выбора комнаты.");
                conversation.Clear();
                return;
            }

            // Сохраняем выбранную комнату
            conversation.SetValue("roomId", roomId);

            // Переходим к вводу даты
            conversation.State = ConversationState.AwaitingBookingDate;
            await _messageSender.SendMessageAsync(chatId,
                "Отлично! Теперь введите дату бронирования.\n\nФормат: dd.MM.yyyy\nПример: 25.05.2026",
                replyMarkup: KeyboardHelper.GetCancelKeyboard());
        }

        public async Task HandleBookingDateInput(long chatId, string messageText, UserConversationData conversation)
        {
            if (!DateTime.TryParseExact(messageText, "dd.MM.yyyy", null,
                System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "❌ Неверный формат даты!\n\nИспользуйте: dd.MM.yyyy\nПример: 25.05.2026");
                return;
            }

            if (date.Date < DateTime.Today)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "❌ Нельзя бронировать на прошедшую дату!");
                return;
            }

            // Сохраняем дату
            conversation.SetValue("date", date);

            // Переходим к вводу времени начала
            conversation.State = ConversationState.AwaitingBookingStartTime;
            await _messageSender.SendMessageAsync(chatId,
                "Введите время начала бронирования.\n\nФормат: HH:mm\nПример: 14:30",
                replyMarkup: KeyboardHelper.GetCancelKeyboard());
        }

        public async Task HandleBookingStartTimeInput(long chatId, string messageText, UserConversationData conversation)
        {
            if (!TimeSpan.TryParseExact(messageText, "hh\\:mm", null, out TimeSpan startTime))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "❌ Неверный формат времени!\n\nИспользуйте: HH:mm\nПример: 14:30");
                return;
            }

            // Сохраняем время начала
            conversation.SetValue("startTime", startTime);

            // Переходим к вводу времени окончания
            conversation.State = ConversationState.AwaitingBookingEndTime;
            await _messageSender.SendMessageAsync(chatId,
                "Введите время окончания бронирования.\n\nФормат: HH:mm\nПример: 16:30",
                replyMarkup: KeyboardHelper.GetCancelKeyboard());
        }

        public async Task HandleBookingEndTimeInput(long chatId, string messageText, UserConversationData conversation)
        {
            if (!TimeSpan.TryParseExact(messageText, "hh\\:mm", null, out TimeSpan endTime))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "❌ Неверный формат времени!\n\nИспользуйте: HH:mm\nПример: 16:30");
                return;
            }

            // Получаем сохраненные данные
            var roomId = conversation.GetValue<int>("roomId");
            var date = conversation.GetValue<DateTime>("date");
            var startTime = conversation.GetValue<TimeSpan>("startTime");

            // Проверяем, что время окончания больше времени начала
            if (endTime <= startTime)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "❌ Время окончания должно быть позже времени начала!");
                return;
            }

            // Собираем полные DateTime
            var timeBegin = date.Add(startTime);
            var timeEnd = date.Add(endTime);

            // Получаем userId из токена
            var tokens = await _sessionService.GetTokensAsync(chatId);
            var claims = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                .ReadJwtToken(tokens.accessToken).Claims;
            var userIdString = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdString, out Guid userId))
            {
                await _messageSender.SendMessageAsync(chatId, "Ошибка авторизации.");
                conversation.Clear();
                return;
            }

            // Создаем бронирование
            var createBookingRequest = new BookingRequest
            {
                Description = "Бронирование комнаты",
                UserId = userId,
                RoomId = roomId,
                Status = BookingStatus.NotConfirmed,
                TimeBegin = timeBegin.ToUniversalTime(),
                TimeEnd =   timeEnd.ToUniversalTime()
            };

            try
            {
                var response = await SendRequestAsync(HttpMethod.Post,
                    $"{_servicesSettings.BookingServiceUrl}/api/booking",
                    chatId,
                    createBookingRequest);

                if (response.IsSuccessStatusCode)
                {
                    await _messageSender.SendMessageAsync(chatId,
                        $"✅ Бронирование создано!\n\n" +
                        $"📅 Дата: {date:dd.MM.yyyy}\n" +
                        $"🕐 Время: {startTime:hh\\:mm} - {endTime:hh\\:mm}");

                    await LogToServiceAsync("Information", "create-booking",
                        $"Booking created for room {roomId}");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId,
                        $"❌ Ошибка создания бронирования: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create booking");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером.");
            }
            finally
            {
                // Очищаем состояние диалога
                conversation.Clear();
            }
        }

        public async Task HandleCancelMyBookingInput(long chatId, UserConversationData conversation)
        {
            if (!await IsPermitted(chatId, UserRole.Student))
            {
                conversation.Clear();
                return;
            }

            try
            {
                var token = await _sessionService.GetTokensAsync(chatId);
                var claims = new JwtSecurityTokenHandler().ReadJwtToken(token.accessToken).Claims;
                var tokenId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                var response = await SendRequestAsync(HttpMethod.Get,
                    $"{_servicesSettings.BookingServiceUrl}/api/booking/user/{tokenId}", chatId);
                var body = await response.Content.ReadAsStringAsync();
                var bookings = JsonSerializer.Deserialize<List<BookingResponse>>(body);

                if (bookings == null || bookings.Count == 0)
                {
                    await _messageSender.SendMessageAsync(chatId, "Нет активных бронирований.");
                    conversation.Clear();
                    return;
                }

                bookings = bookings
                    .Where(b => b.Status != BookingStatus.Canceled && b.Status != BookingStatus.Completed)
                    .OrderBy(b => b.Period?.TimeBegin)
                    .ToList();

                if (bookings.Count == 0)
                {
                    await _messageSender.SendMessageAsync(chatId, "Нет активных бронирований.");
                    conversation.Clear();
                    return;
                }

                var roomNames = await GetRoomNamesAsync(bookings.Select(b => b.RoomId).Distinct(), chatId);

                conversation.State = ConversationState.AwaitingCancelBookingSelection;

                var keyboard = KeyboardHelper.GetCancelBookingSelectionKeyboard(bookings, roomNames);
                await _messageSender.SendMessageAsync(chatId,
                    "❌ Отмена бронирования\n\nВыберите бронирование:",
                    replyMarkup: keyboard);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to BookingService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                conversation.Clear();
            }
        }

        public async Task HandleCancelBookingSelectionCallback(long chatId, string callbackData)
        {
            var conversation = _conversationService.GetOrCreate(chatId);

            if (conversation.State != ConversationState.AwaitingCancelBookingSelection)
            {
                conversation.Clear();
                await _messageSender.SendMessageAsync(chatId, "Ошибка состояния. Начните заново.");
                return;
            }

            var bookingIdString = callbackData.Replace("cancelbook_", "");
            if (!Guid.TryParse(bookingIdString, out Guid bookingId))
            {
                await _messageSender.SendMessageAsync(chatId, "Ошибка выбора бронирования.");
                conversation.Clear();
                return;
            }

            conversation.SetValue("bookingId", bookingId);
            conversation.State = ConversationState.AwaitingCancelBookingConfirmation;

            await _messageSender.SendMessageAsync(chatId,
                "Вы уверены, что хотите отменить бронирование?",
                replyMarkup: KeyboardHelper.GetCancelBookingConfirmKeyboard(bookingId));
        }

        public async Task HandleConfirmCancelBookingCallback(long chatId, string callbackData)
        {
            var conversation = _conversationService.GetOrCreate(chatId);

            if (conversation.State != ConversationState.AwaitingCancelBookingConfirmation)
            {
                conversation.Clear();
                await _messageSender.SendMessageAsync(chatId, "Ошибка состояния. Начните заново.");
                return;
            }

            var bookingIdString = callbackData.Replace("confirmcancel_", "");
            if (!Guid.TryParse(bookingIdString, out Guid bookingId))
            {
                await _messageSender.SendMessageAsync(chatId, "Ошибка. Попробуйте заново.");
                conversation.Clear();
                return;
            }

            try
            {
                var getResponse = await SendRequestAsync(HttpMethod.Get,
                    $"{_servicesSettings.BookingServiceUrl}/api/booking/booking/{bookingId}", chatId);

                if (!getResponse.IsSuccessStatusCode)
                {
                    await _messageSender.SendMessageAsync(chatId, "Ошибка: бронирование не найдено.");
                    conversation.Clear();
                    return;
                }

                var body = await getResponse.Content.ReadAsStringAsync();
                var booking = JsonSerializer.Deserialize<BookingResponse>(body);

                if (booking == null)
                {
                    await _messageSender.SendMessageAsync(chatId, "Ошибка получения данных бронирования.");
                    conversation.Clear();
                    return;
                }

                var updateRequest = new BookingRequest
                {
                    Id = bookingId,
                    Description = booking.Description ?? string.Empty,
                    UserId = booking.UserId,
                    RoomId = booking.RoomId,
                    Status = BookingStatus.Canceled,
                    TimeBegin = booking.Period?.TimeBegin,
                    TimeEnd = booking.Period?.TimeEnd
                };

                var putResponse = await SendRequestAsync(HttpMethod.Put,
                    $"{_servicesSettings.BookingServiceUrl}/api/booking",
                    chatId,
                    updateRequest);

                if (putResponse.IsSuccessStatusCode)
                {
                    await _messageSender.SendMessageAsync(chatId, "✅ Бронирование отменено.");
                    await LogToServiceAsync("Information", "cancel-booking", $"Booking {bookingId} canceled by user");
                }
                else
                {
                    var error = await putResponse.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"❌ Ошибка: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel booking");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером.");
            }
            finally
            {
                conversation.Clear();
            }
        }
    }
}
