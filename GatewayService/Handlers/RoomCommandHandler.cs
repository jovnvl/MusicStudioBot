using GatewayService.Configuration;
using GatewayService.DTO;
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
        private const int CreateRoomCategoryCommandPartsCount = 2;
        private const int CreateRoomCommandPartsCount = 3;
        private const int UpdateRoomCommandMinPartsCount = 2;
        private const int GetRoomCommandMinPartsCount = 1;

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

        public async Task HandleCreateRoomCommand(long chatId, string messageText)
        {
            if (!await IsPermitted(chatId, UserRole.Administrator))
                return;
            string[] createRoomCommand = messageText.Split('|');
            if (createRoomCommand.Length < CreateRoomCommandPartsCount || !int.TryParse(createRoomCommand[1], out int category_id))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /create_room name | category_id | description");
                return;
            }

            string name = createRoomCommand[0].Substring(createRoomCommand[0].IndexOf(' ') + 1).Trim();
            string description = createRoomCommand[2].Trim();

            var createRoomRequest = new CreateRoomRequest
            {
                Name = name,
                CategoryRoomId = category_id,
                Description = description,
                Photo = null,
                Status = 0,
            };

            try
            {
                var response = await SendRequestAsync(HttpMethod.Post, $"{_servicesSettings.RoomServiceUrl}/api/rooms", chatId, createRoomRequest);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var roomResponse = JsonSerializer.Deserialize<RoomResponse>(body);
                    if (roomResponse == null)
                    {
                        _logger.LogError("Failed to deserialize RoomResponse");
                        await LogToServiceAsync("Error", "room-response-fail", "Failed to deserialize RoomResponse");
                        return;
                    }
                    _logger.LogInformation("Added new room. Room:{Name}, {CategoryRoomId}", roomResponse.Name, roomResponse.CategoryRoomId);
                    await LogToServiceAsync("Information", "create-room", $"Added new room. Room: {roomResponse.Name}, {roomResponse.CategoryRoomId}");
                    await _messageSender.SendMessageAsync(chatId, "Комната добавлена");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "create-room-fail", $"Receipt of rooms information failed : {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-fail", "HTTP request to RoomService failed");
            }
        }

        public async Task HandleUpdateRoomCommand(long chatId, string messageText)
        {
            if (!await IsPermitted(chatId, UserRole.Moderator))
                return;
            string[] updateRoomCommand = messageText.Split(' ');
            if (updateRoomCommand.Length < UpdateRoomCommandMinPartsCount || !int.TryParse(updateRoomCommand[1], out int id) || !RoomStatus.TryParse(updateRoomCommand[2], out RoomStatus status))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /update_room id status");
                return;
            }

            var updateRoomRequest = new UpdateRoomRequest
            {
                Id = id,
                Status = status,
            };

            try
            {
                var response = await SendRequestAsync(HttpMethod.Put, $"{_servicesSettings.RoomServiceUrl}/api/rooms", chatId, updateRoomRequest);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successful update of room information.");
                    await LogToServiceAsync("Information", "update-room", "Successful update of room information.");
                    var message = "Статус комнаты обновлен";
                    await _messageSender.SendMessageAsync(chatId, message);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "update-room-fail", $"Update of room information failed : {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-fail", "HTTP request to RoomService failed");
            }
        }

        public async Task HandleCreateRoomCategoryCommand(long chatId, string messageText)
        {
            if (!await IsPermitted(chatId, UserRole.Administrator))
                return;
            string[] createRoomCategoryCommand = messageText.Split('|');
            if (createRoomCategoryCommand.Length < CreateRoomCategoryCommandPartsCount)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /create_room_category name | description");
                return;
            }
            string name = createRoomCategoryCommand[0].Substring(createRoomCategoryCommand[0].IndexOf(' ') + 1).Trim();
            string description = createRoomCategoryCommand[1].Trim();

            var createRoomCategoryRequest = new CreateRoomCategoryRequest
            {
                Name = name,
                Description = description,
            };

            try
            {
                var response = await SendRequestAsync(HttpMethod.Post, $"{_servicesSettings.RoomServiceUrl}/api/categories", chatId, createRoomCategoryRequest);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var roomCategoryResponse = JsonSerializer.Deserialize<RoomCategoryResponse>(body);
                    if (roomCategoryResponse == null)
                    {
                        _logger.LogError("Failed to deserialize RoomCategoryResponse");
                        await LogToServiceAsync("Error", "room-categ-resp-fail", "Failed to deserialize RoomCategoryResponse");
                        return;
                    }
                    _logger.LogInformation("Added new room category. Room Category:{Id}, {Name}", roomCategoryResponse.Id, roomCategoryResponse.Name);
                    await LogToServiceAsync("Information", "create-category", $"Added new room category. Room Category: {roomCategoryResponse.Id}, {roomCategoryResponse.Name}");
                    await _messageSender.SendMessageAsync(chatId, "Категория добавлена");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "create-category-fail", $"Failed to add new room category: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-failed", "HTTP request to RoomService failed");
            }
        }

        public async Task HandleGetRoomCommand(long chatId, string messageText)
        {
            if (!await IsPermitted(chatId, UserRole.Moderator))
                return;
            string[] getRoomCommand = messageText.Split(' ');
            if (getRoomCommand.Length < GetRoomCommandMinPartsCount || !int.TryParse(getRoomCommand[1], out int id))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /get_room id");
                return;
            }

            try
            {
                var response = await SendRequestAsync(HttpMethod.Get, $"{_servicesSettings.RoomServiceUrl}/api/rooms/{id}", chatId);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var roomResponse = JsonSerializer.Deserialize<RoomResponse>(body);
                    if (roomResponse == null)
                    {
                        _logger.LogError("Failed to deserialize RoomResponse");
                        await LogToServiceAsync("Error", "room-response-fail", "Failed to deserialize RoomResponse");
                        await _messageSender.SendMessageAsync(chatId, "Такой комнаты нет");
                        return;
                    }
                    _logger.LogInformation("Successful receipt of room with Id: {Id}  information.", roomResponse.Id);
                    await LogToServiceAsync("Information", "get-room", $"Successful receipt of room with Id: {roomResponse.Id}  information.");
                    var statusEmoji = MapRoomStatusToEmoji(roomResponse.Status);
                    var message = $"{statusEmoji} {roomResponse.Name} (id:{roomResponse.Id})\n";
                    message += $"  └ {roomResponse.Description}\n\n";
                    await _messageSender.SendMessageAsync(chatId, message);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "get-room-fail", $"Failed to get room info: {errorMessage}");
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
