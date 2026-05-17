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
    public class RoomCommandHandler : CommandHandler
    {
        private readonly ServicesSettings _servicesSettings;
        private const int CreateRoomCategoryCommandPartsCount = 2;
        private const int CreateRoomCommandPartsCount = 3;
        private const int UpdateRoomCommandMinPartsCount = 2;
        private const int GetRoomCommandMinPartsCount = 1;

        public RoomCommandHandler(IHttpClientFactory httpClientFactory, ILogger<CommandHandler> logger, IUserSessionService sessionService, IMessageSender messageSender, IRabbitMQPublisher rabbitMQPublisher, IOptions<ServicesSettings> servicesSettings) : base(httpClientFactory, logger, sessionService, messageSender, rabbitMQPublisher)
        {
            _servicesSettings = servicesSettings.Value;
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
                        var statusEmoji = MapToEmojiStatus(room.Status);

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
                    var statusEmoji = MapToEmojiStatus(roomResponse.Status);
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
    }
}
