using GatewayService.Configuration;
using GatewayService.Models.Conversation;
using GatewayService.Models.DTOs;
using GatewayService.Models.Enums;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using GatewayService.Services.Telegram;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GatewayService.Handlers
{
    public class RoomCommandHandler : CommandHandler
    {
        private readonly ServicesSettings _servicesSettings;
        private readonly IConversationStateService _conversationService;

        public RoomCommandHandler(
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

        protected string MapRoomStatusToEmoji(RoomStatus? status)
        {
            return status switch
            {
                RoomStatus.Available => "✅",
                RoomStatus.Occupied => "🔴",
                RoomStatus.Reserved => "🟡",
                RoomStatus.Maintenance => "🔧",
                _ => "❓"
            };
        }

        public async Task HandleGetRoomsCommand(long chatId)
        {
            //if (!await IsPermitted(chatId, UserRole.Moderator))
            //    return;

            try
            {
                var response = await SendRequestAsync(HttpMethod.Get, $"{_servicesSettings.RoomServiceUrl}/api/rooms", chatId);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var rooms = JsonSerializer.Deserialize<List<RoomResponse>>(body);
                    if (rooms == null || rooms.Count == 0)
                    {
                        _logger.LogError("Failed to deserialize RoomResponse");
                        await LogToServiceAsync("Error", "room-response-fail", "Failed to deserialize RoomResponse");
                        await _messageSender.SendMessageAsync(chatId, "Комнат пока нет");
                        return;
                    }
                    _logger.LogInformation("Successful receipt of rooms information.");
                    await LogToServiceAsync("Information", "get-rooms", "Successful receipt of rooms information.");
                    var message = "Доступные комнаты:\n\n";
                    foreach (var room in rooms)
                    {
                        var statusEmoji = MapRoomStatusToEmoji(room.Status);

                        message += $"{statusEmoji} {room.Name} (id:{room.Id})\n";
                        message += $"  └ {room.Description}\n\n";
                    }
                    await _messageSender.SendMessageAsync(chatId, message);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "get-rooms-fail", $"Receipt of rooms information failed : {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-fail", "HTTP request to RoomService failed");
            }
        }

        public async Task HandleUpdateRoomStatusInput(long chatId, UserConversationData conversation)
        {
            if (!await IsPermitted(chatId, UserRole.Moderator))
            {
                conversation.Clear();
                return;
            }

            try
            {
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
                    await _messageSender.SendMessageAsync(chatId, "Комнат пока нет.");
                    conversation.Clear();
                    return;
                }

                conversation.State = ConversationState.AwaitingRoomStatusSelection;

                await _messageSender.SendMessageAsync(chatId,
                    "Смена статуса комнаты\n\nВыберите комнату:",
                    replyMarkup: KeyboardHelper.GetRoomStatusSelectionKeyboard(rooms));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load rooms for status update");
                await _messageSender.SendMessageAsync(chatId, "Ошибка. Попробуйте позже.");
                conversation.Clear();
            }
        }

        public async Task HandleRoomStatusSelectionCallback(long chatId, string callbackData)
        {
            var conversation = _conversationService.GetOrCreate(chatId);

            if (conversation.State != ConversationState.AwaitingRoomStatusSelection)
            {
                conversation.Clear();
                await _messageSender.SendMessageAsync(chatId, "Ошибка состояния. Начните заново.");
                return;
            }

            var roomIdString = callbackData.Replace("roomstatus_", "");
            if (!int.TryParse(roomIdString, out int roomId))
            {
                await _messageSender.SendMessageAsync(chatId, "Ошибка выбора комнаты.");
                conversation.Clear();
                return;
            }

            conversation.SetValue("roomId", roomId);
            conversation.State = ConversationState.AwaitingRoomStatusConfirmation;

            await _messageSender.SendMessageAsync(chatId,
                "Выберите новый статус:",
                replyMarkup: KeyboardHelper.GetRoomNewStatusKeyboard());
        }

        public async Task HandleRoomNewStatusCallback(long chatId, string callbackData)
        {
            var conversation = _conversationService.GetOrCreate(chatId);

            if (conversation.State != ConversationState.AwaitingRoomStatusConfirmation)
            {
                conversation.Clear();
                await _messageSender.SendMessageAsync(chatId, "Ошибка состояния. Начните заново.");
                return;
            }

            var statusString = callbackData.Replace("newroomstatus_", "");
            if (!Enum.TryParse<RoomStatus>(statusString, out var newStatus))
            {
                await _messageSender.SendMessageAsync(chatId, "Ошибка выбора статуса.");
                conversation.Clear();
                return;
            }

            var roomId = conversation.GetValue<int>("roomId");

            try
            {
                var updateRequest = new UpdateRoomRequest { Id = roomId, Status = newStatus };
                var response = await SendRequestAsync(HttpMethod.Put,
                    $"{_servicesSettings.RoomServiceUrl}/api/rooms", chatId, updateRequest);

                if (response.IsSuccessStatusCode)
                {
                    await _messageSender.SendMessageAsync(chatId, "✅ Статус комнаты обновлён!");
                    await LogToServiceAsync("Information", "update-room-status",
                        $"Room {roomId} status changed to {newStatus}");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"❌ Ошибка: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update room status");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером.");
            }
            finally
            {
                conversation.Clear();
            }
        }
    }
}
