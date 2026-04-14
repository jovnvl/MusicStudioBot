using GatewayService.Configuration;
using GatewayService.DTO;
using GatewayService.Models.DTOs;
using GatewayService.Models.Enums;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace GatewayService.Handlers
{
    public class CommandHandler : ICommandHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CommandHandler> _logger;
        private readonly ServicesSettings _servicesSettings;
        private readonly IUserSessionService _sessionService;
        private readonly IMessageSender _messageSender;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        private const int RegisterCommandPartsCount = 5;
        private const int LoginCommandPartsCount = 2;
        private const int UpdateCommandMinPartsCount = 2;
        private const int CreateRoomCategoryCommandPartsCount = 2;
        private const int CreateRoomCommandPartsCount = 3;
        private const int UpdateRoomCommandMinPartsCount = 2;
        private const int GetRoomCommandMinPartsCount = 1;
        private const int ChangeUserRoleCommandPartsCount = 3;
        private const int CreateBookingCommandPartsCount = 3;

        public CommandHandler(
            IHttpClientFactory httpClientFactory, 
            ILogger<CommandHandler> logger, 
            IOptions<ServicesSettings> servicesSettings, 
            IUserSessionService sessionService, 
            IMessageSender messageSender, 
            IRabbitMQPublisher rabbitMQPublisher
            )
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _servicesSettings = servicesSettings.Value;
            _sessionService = sessionService;
            _messageSender = messageSender;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        private string MapToEmojiStatus(int status)
        {
            return status switch
            {
                0 => "✅", // Available
                1 => "🔴", // Occupied
                2 => "🟡", // Reserved
                3 => "🔧", // Maintenance
                _ => "❓"
            };
        }

        private async Task<bool> IsPermitted(long chatId, UserRole role)
        {
            var token = _sessionService.GetToken(chatId);
            if (string.IsNullOrEmpty(token))
            {
                await _messageSender.SendMessageAsync(chatId, "Вы не авторизованы.");
                return false;
            }
            if (!HasRole(token, role))
            {
                await _messageSender.SendMessageAsync(chatId, "Недостаточно прав.");
                return false;
            }
            return true;
        }

        private bool HasRole(string token, UserRole requiredRole)
        {
            var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;
            var tokenRole = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (!Enum.TryParse<UserRole>(tokenRole, out var userRole))
                return false;
            return (int)userRole >= (int)requiredRole;
        }

        private async Task LogToServiceAsync(string level, string eventType, string message)
        {
            await _rabbitMQPublisher.PublishAsync("logging_service_queue", new LogEventDto(level, eventType, message));
        }

        private async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string endpoint, object request)
        {
            var httpClient = _httpClientFactory.CreateClient();
            StringContent? content = null;
            if (request != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
            {
                var json = JsonSerializer.Serialize(request);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            if (method == HttpMethod.Post)
            {
                return await httpClient.PostAsync(endpoint, content);
            }
            if (method == HttpMethod.Get)
            {
                return await httpClient.GetAsync(endpoint);
            }
            if (method == HttpMethod.Put)
            {
                return await httpClient.PutAsync(endpoint, content);
            }
            if (method == HttpMethod.Delete)
            {
                return await httpClient.DeleteAsync(endpoint);
            }
            throw new NotImplementedException($"HTTP method {method} is not supported.");
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
/update_room_status - Обновить статус (формат: /update_room_status id status)";
            await _messageSender.SendMessageAsync(chatId, helpMessage);
        }

        public async Task HandleRegisterCommand(long chatId, string messageText)
        {
            string[] registerCommand = messageText.Split(' ');
            if (registerCommand.Length < RegisterCommandPartsCount)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /register username password firstName lastName");
                return;
            }
            string username = registerCommand[1];
            string password = registerCommand[2];
            string firstName = registerCommand[3];
            string lastName = registerCommand[4];
            long telegramId = chatId;

            var registerRequest = new RegisterRequest
            {
                Username = username,
                Password = password,
                FirstName = firstName,
                LastName = lastName,
                TelegramId = telegramId,
            };

            try
            {
                var response = await SendRequestAsync(HttpMethod.Post, $"{_servicesSettings.IdentityServiceUrl}/api/auth/register", registerRequest);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(body);
                    if (authResponse == null)
                    {
                        _logger.LogError("Failed to deserialize AuthResponse");
                        await LogToServiceAsync("Error", "auth-response-deserialize-failed", "Failed to deserialize AuthResponse");
                        return;
                    }
                    _logger.LogInformation("Registration successful. New user id: {UserId}", authResponse.UserId);
                    await LogToServiceAsync("Information", "user-registered", $"Registration successful. New user id: {authResponse.UserId}");
                    await _messageSender.SendMessageAsync(chatId, "Успешная регистрация! Теперь вы можете использовать /login");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка регистрации: {errorMessage}");
                    await LogToServiceAsync("Error", "register-failed", $"Register failed, ChatId: {chatId}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-identity-failed", "HTTP request to IdentityService failed");
            }
        }

        public async Task HandleLoginCommand(long chatId, string messageText)
        {
            string[] loginCommand = messageText.Split(' ');
            if (loginCommand.Length < LoginCommandPartsCount)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /login 'password'");
                return;
            }
            string password = loginCommand[1];

            var httpClient = _httpClientFactory.CreateClient();
            var identityServiceUrl = _servicesSettings.IdentityServiceUrl;

            var loginRequest = new LoginRequest
            {
                Password = password,
                TelegramId = chatId,
            };

            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{identityServiceUrl}/api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(body);
                    if (authResponse == null)
                    {
                        _logger.LogError("Failed to deserialize AuthResponse");
                        await LogToServiceAsync("Error", "auth-response-deserialize-failed", "Failed to deserialize AuthResponse");
                        return;
                    }
                    _logger.LogInformation("Login successful. User token: {UserId}", authResponse.UserId);
                    await LogToServiceAsync("Information", "login-successful", $"Login successful. User id: {authResponse.UserId}");
                    _sessionService.SaveToken(chatId, authResponse.Token);
                    await _messageSender.SendMessageAsync(chatId, "Успешный вход.");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка входа: {errorMessage}");
                    await LogToServiceAsync("Error", "login-failed", $"Login failed: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-identity-failed", "HTTP request to IdentityService failed");
            }
        }

        public async Task HandleMyProfileCommand(long chatId)
        {
            var token = _sessionService.GetToken(chatId);
            if (string.IsNullOrEmpty(token))
            {
                await _messageSender.SendMessageAsync(chatId, "Вы не авторизованы. Используйте /login");
                return;
            }
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var identityServiceUrl = _servicesSettings.IdentityServiceUrl;
            try
            {
                var response = await httpClient.GetAsync($"{identityServiceUrl}/api/auth/user/telegram/{chatId}");

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var userResponse = JsonSerializer.Deserialize<UserResponse>(body);
                    if (userResponse == null)
                    {
                        _logger.LogError("Failed to deserialize UserResponse");
                        await LogToServiceAsync("Error", "user-response-deserialize-failed", "Failed to deserialize UserResponse");
                        return;
                    }
                    _logger.LogInformation("Successful receipt of user information. User: {Id}, {Username}", userResponse.Id, userResponse.Username);
                    await LogToServiceAsync("Information", "get-user-info", $"Successful receipt of user information. User: {userResponse.Id}, {userResponse.Username}");
                    await _messageSender.SendMessageAsync(chatId, $"Id: {userResponse.Id}\nЛогин: {userResponse.Username}\nИмя: {userResponse.FirstName}\nФамилия: {userResponse.LastName}");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "get-user-info-failed", $"Receipt of user information failed: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-identity-failed", "HTTP request to IdentityService failed");
            }
        }

        public async Task HandleUpdateProfileCommand(long chatId, string messageText)
        {
            var token = _sessionService.GetToken(chatId);
            if (string.IsNullOrEmpty(token))
            {
                await _messageSender.SendMessageAsync(chatId, "Вы не авторизованы. Используйте /login");
                return;
            }

            string[] updateCommand = messageText.Split(' ');
            if (updateCommand.Length < UpdateCommandMinPartsCount)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /update_profile [username] [firstName] [lastName]\n" +
                    "Можно указать только нужные параметры, используя - для пропуска:\n" +
                    "Пример: /update_profile new_user - NewLastName");
                return;
            }

            var updateProfileRequest = new UpdateProfileRequest
            {
                Username = updateCommand.Length > 1 && updateCommand[1] != "-" ? updateCommand[1] : null,
                FirstName = updateCommand.Length > 2 && updateCommand[2] != "-" ? updateCommand[2] : null,
                LastName = updateCommand.Length > 3 && updateCommand[3] != "-" ? updateCommand[3] : null
            };

            if (updateProfileRequest.Username == null &&
                updateProfileRequest.FirstName == null &&
                updateProfileRequest.LastName == null)
            {
                await _messageSender.SendMessageAsync(chatId, "Укажите хотя бы один параметр для обновления!");
                return;
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);  
            var identityServiceUrl = _servicesSettings.IdentityServiceUrl;

            var json = JsonSerializer.Serialize(updateProfileRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PutAsync($"{identityServiceUrl}/api/auth/user/update_profile", content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var userResponse = JsonSerializer.Deserialize<UserResponse>(body);
                    if (userResponse == null)
                    {
                        _logger.LogError("Failed to deserialize UserResponse");
                        await LogToServiceAsync("Error", "user-response-deserialize-failed", "Failed to deserialize UserResponse");
                        return;
                    }
                    _logger.LogInformation("Successful update of user information. User: {Id}, {Username}", userResponse.Id, userResponse.Username);
                    await LogToServiceAsync("Information", "user-info-update", $"Successful update of user information. User: {userResponse.Id}, {userResponse.Username}");
                    await _messageSender.SendMessageAsync(chatId, $"Id: {userResponse.Id}\nЛогин: {userResponse.Username}\nИмя: {userResponse.FirstName}\nФамилия: {userResponse.LastName}");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "user-info-update-failed", $"Update of user information failed: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-identity-failed", "HTTP request to IdentityService failed");
            }
        }

        public async Task HandleGetRoomsCommand(long chatId)
        {
            if (!await IsPermitted(chatId, UserRole.Moderator))
                return;
            var httpClient = _httpClientFactory.CreateClient();
            var roomServiceUrl = _servicesSettings.RoomServiceUrl;
            try
            {
                var response = await httpClient.GetAsync($"{roomServiceUrl}/api/rooms");

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var rooms = JsonSerializer.Deserialize<List<RoomResponse>>(body);
                    if (rooms == null || rooms.Count == 0)
                    {
                        _logger.LogError("Failed to deserialize RoomResponse");
                        await LogToServiceAsync("Error", "room-response-deserialize-failed", "Failed to deserialize RoomResponse");
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
                    await LogToServiceAsync("Error", "get-rooms-failed", $"Receipt of rooms information failed : {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-room-failed", "HTTP request to RoomService failed");
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

            var httpClient = _httpClientFactory.CreateClient();
            var roomServiceUrl = _servicesSettings.RoomServiceUrl;

            var createRoomRequest = new CreateRoomRequest
            {
                Name = name,
                CategoryRoomId = category_id,
                Description = description,
                Photo = null,
                Status = 0,
            };

            var json = JsonSerializer.Serialize(createRoomRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{roomServiceUrl}/api/rooms", content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var roomResponse = JsonSerializer.Deserialize<RoomResponse>(body);
                    if (roomResponse == null)
                    {
                        _logger.LogError("Failed to deserialize RoomResponse");
                        await LogToServiceAsync("Error", "room-response-deserialize-failed", "Failed to deserialize RoomResponse");
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
                    await LogToServiceAsync("Error", "create-room-failed", $"Receipt of rooms information failed : {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-room-failed", "HTTP request to RoomService failed");
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

            var httpClient = _httpClientFactory.CreateClient();
            var roomServiceUrl = _servicesSettings.RoomServiceUrl;

            var json = JsonSerializer.Serialize(updateRoomRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PutAsync($"{roomServiceUrl}/api/rooms", content);

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
                    await LogToServiceAsync("Error", "update-room-failed", $"Update of room information failed : {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-room-failed", "HTTP request to RoomService failed");
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

            var httpClient = _httpClientFactory.CreateClient();
            var roomServiceUrl = _servicesSettings.RoomServiceUrl;

            var createRoomCategoryRequest = new CreateRoomCategoryRequest
            {
                Name = name,
                Description = description,
            };

            var json = JsonSerializer.Serialize(createRoomCategoryRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{roomServiceUrl}/api/categories", content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var roomCategoryResponse = JsonSerializer.Deserialize<RoomCategoryResponse>(body);
                    if (roomCategoryResponse == null)
                    {
                        _logger.LogError("Failed to deserialize RoomCategoryResponse");
                        await LogToServiceAsync("Error", "room-category-response-deserialize-failed", "Failed to deserialize RoomCategoryResponse");
                        return;
                    }
                    _logger.LogInformation("Added new room category. Room Category:{Id}, {Name}",roomCategoryResponse.Id,roomCategoryResponse.Name);
                    await LogToServiceAsync("Information", "create-room-category", $"Added new room category. Room Category: {roomCategoryResponse.Id}, {roomCategoryResponse.Name}");
                    await _messageSender.SendMessageAsync(chatId, "Категория добавлена");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "create-room-category-failed", $"Failed to add new room category: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-room-failed", "HTTP request to RoomService failed");
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

            var httpClient = _httpClientFactory.CreateClient();
            var roomServiceUrl = _servicesSettings.RoomServiceUrl;
            try
            {
                var response = await httpClient.GetAsync($"{roomServiceUrl}/api/rooms/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var roomResponse = JsonSerializer.Deserialize<RoomResponse>(body);
                    if (roomResponse == null)
                    {
                        _logger.LogError("Failed to deserialize RoomResponse");
                        await LogToServiceAsync("Error", "room-response-deserialize-failed", "Failed to deserialize RoomResponse");
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
                    await LogToServiceAsync("Error", "get-room-failed", $"Failed to get room info: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-room-failed", "HTTP request to RoomService failed");
            }
        }

        public async Task HandleGetUsersCommand(long chatId)
        {
            if (!await IsPermitted(chatId, UserRole.Moderator))
                return;
            var httpClient = _httpClientFactory.CreateClient();
            var identityServiceUrl = _servicesSettings.IdentityServiceUrl;
            try
            {
                var response = await httpClient.GetAsync($"{identityServiceUrl}/api/auth/user/all");

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<UserResponse>>(body);
                    if (users == null || users.Count == 0)
                    {
                        _logger.LogError("Failed to deserialize UserResponse");
                        await LogToServiceAsync("Error", "user-response-deserialize-failed", "Failed to deserialize UserResponse");
                        await _messageSender.SendMessageAsync(chatId, "Пользователей пока нет");
                        return;
                    }
                    _logger.LogInformation("Successful receipt of users information.");
                    await LogToServiceAsync("Information", "get-users", "Successful receipt of users information.");
                    var message = "Список пользователей:\n\n";
                    foreach (var user in users)
                    {

                        message += $"* {user.Username} (id:{user.Id}, role: {user.Role})\n";
                        message += $"   └ {user.LastName} {user.FirstName}\n\n";
                    }
                    await _messageSender.SendMessageAsync(chatId, message);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "get-users-failed", $"Failed to get users info: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-identity-failed", "HTTP request to IdentityService failed");
            }
        }

        public async Task HandleChangeUserRoleCommand(long chatId, string messageText)
        {
            if (!await IsPermitted(chatId, UserRole.Administrator))
                return;
            string[] changeUserRoleCommand = messageText.Split(' ');
            if (changeUserRoleCommand.Length < ChangeUserRoleCommandPartsCount)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /change_user_role id role");
                return;
            }

            if (!Guid.TryParse(changeUserRoleCommand[1], out Guid id))
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный id.");
                return;
            }

            string role = changeUserRoleCommand[2];

            var httpClient = _httpClientFactory.CreateClient();
            var identityServiceUrl = _servicesSettings.IdentityServiceUrl;

            var changeRoleRequest = new ChangeRoleRequest
            {
                Id = id,
                Role = role,
            };

            var json = JsonSerializer.Serialize(changeRoleRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var response = await httpClient.PutAsync($"{identityServiceUrl}/api/auth/user/change_role", content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var userResponse = JsonSerializer.Deserialize<UserResponse>(body);
                    if (userResponse == null)
                    {
                        _logger.LogError("Failed to deserialize UserResponse");
                        await LogToServiceAsync("Error", "user-response-deserialize-failed", "Failed to deserialize UserResponse");
                        await _messageSender.SendMessageAsync(chatId, "Такого пользователя нет");
                        return;
                    }
                    _logger.LogInformation("User {Id} role successfully changed to {Role}.", userResponse.Id, userResponse.Role);
                    await LogToServiceAsync("Information", "change-user-role", $"User {userResponse.Id} role successfully changed to {userResponse.Role}.");
                    var message = "Роль пользователя изменена";
                    await _messageSender.SendMessageAsync(chatId, message);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "change-user-role-failed", $"Failed to change user role: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-to-identity-failed", "HTTP request to IdentityService failed");
            }
        }

        public async Task HandleCreateBookingCommand(long chatId, string messageText)
        {
            if (!await IsPermitted(chatId, UserRole.Moderator))
                return;
            string[] createBookingCommand = messageText.Split('|');
            if (createBookingCommand.Length < CreateBookingCommandPartsCount)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /new_booking userId | roomId | timeBegin | timeEnd");
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

            var httpClient = _httpClientFactory.CreateClient();
            var bookingServiceUrl = _servicesSettings.BookingServiceUrl;

            var createBookingRequest = new CreateBookingRequest
            {
                Description = "Бронирование комнаты",
                UserId = userId,
                RoomId = roomId,
                Status = BookingStatus.NotConfirmed,
                TimeBegin = timeBegin,
                TimeEnd = timeEnd,
            };

            var json = JsonSerializer.Serialize(createBookingRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{bookingServiceUrl}/api/bookings", content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var roomCategoryResponse = JsonSerializer.Deserialize<BookingResponse>(body);
                    if (roomCategoryResponse == null)
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
            throw new NotImplementedException();
        }
    }
}
