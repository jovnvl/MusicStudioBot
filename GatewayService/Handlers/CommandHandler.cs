using GatewayService.Configuration;
using GatewayService.DTO;
using GatewayService.Models.DTOs;
using GatewayService.Models.Enums;
using GatewayService.Models.Events;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Net.NetworkInformation;
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

        public async Task HandleStartCommand(long chatId)
        {
            string welcomeMessage = @"
Приветствуем в Music Studio Bot! 🎵
   
Этот бот поможет вам забронировать комнату для репетиций
            ";
            await _messageSender.SendMessageAsync(chatId, welcomeMessage);
            _logger.LogInformation("Sent start command response to ChatId: {ChatId}", chatId);
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
/rooms - Получить информацию о комнатах
/create_room - Создать комнату (формат: /create_room name | category_id | description)
/create_room_category - Создать категорию (формат: /create_room_category name | description)
/get_room - Получить комнату (формат: /get_room id)
/update_room_status - Обновить статус (формат: /update_room_status id status)";
            await _messageSender.SendMessageAsync(chatId, helpMessage);
            _logger.LogInformation("Sent help command response to ChatId: {ChatId}", chatId);
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

            var httpClient = _httpClientFactory.CreateClient();
            var identityServiceUrl = _servicesSettings.IdentityServiceUrl;

            var registerRequest = new RegisterRequest
            {
                Username = username,
                Password = password,
                FirstName = firstName,
                LastName = lastName,
                TelegramId = telegramId,
            };

            var json = JsonSerializer.Serialize(registerRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync($"{identityServiceUrl}/api/auth/register", content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(body);
                    if (authResponse == null)
                    {
                        _logger.LogError("Failed to deserialize AuthResponse");
                        return;
                    }
                    _logger.LogInformation("Registration successful. New user token: {Token}", authResponse.Token);

                    await _rabbitMQPublisher.PublishAsync("user_registered_queue", new UserRegisteredEvent
                    {
                        UserId = authResponse.UserId,
                        TelegramId = chatId,
                        Username = authResponse.Username,
                        RegisteredAt = DateTime.UtcNow
                    });

                    await _messageSender.SendMessageAsync(chatId, "Успешная регистрация! Теперь вы можете использовать /login");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка регистрации: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
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
                        return;
                    }
                    _logger.LogInformation("Login successful. User token: {Token}", authResponse.Token);

                    await _rabbitMQPublisher.PublishAsync("user_logged_in_queue", new UserLoggedInEvent
                    {
                        UserId = authResponse.UserId,
                        TelegramId = chatId,
                        Username = authResponse.Username,
                        LoggedInAt = DateTime.UtcNow
                    });

                    _sessionService.SaveToken(chatId, authResponse.Token);
                    await _messageSender.SendMessageAsync(chatId, "Успешный вход.");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка входа: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
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
                        return;
                    }
                    _logger.LogInformation("Successful receipt of user information. User: {Id}, {Username}", userResponse.Id, userResponse.Username);
                    await _messageSender.SendMessageAsync(chatId, $"Id: {userResponse.Id}\nЛогин: {userResponse.Username}\nИмя: {userResponse.FirstName}\nФамилия: {userResponse.LastName}");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
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
                        return;
                    }
                    _logger.LogInformation("Successful update of user information. User: {Id}, {Username}", userResponse.Id, userResponse.Username);
                    await _messageSender.SendMessageAsync(chatId, $"Id: {userResponse.Id}\nЛогин: {userResponse.Username}\nИмя: {userResponse.FirstName}\nФамилия: {userResponse.LastName}");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
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
                        await _messageSender.SendMessageAsync(chatId, "Комнат пока нет");
                        return;
                    }
                    _logger.LogInformation("Successful receipt of rooms information.");
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
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
            }
        }

        public async Task HandleCreateRoomCommand(long chatId, string messageText)
        {
            if (!await IsPermitted(chatId, UserRole.Admin))
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
                        return;
                    }
                    _logger.LogInformation("Added new room. Room:{Name}, {CategoryRoomId}", roomResponse.Name, roomResponse.CategoryRoomId);

                    await _messageSender.SendMessageAsync(chatId, "Комната добавлена");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
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
                    var message = "Статус комнаты обновлен";
                    await _messageSender.SendMessageAsync(chatId, message);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
            }
        }

        public async Task HandleCreateRoomCategoryCommand(long chatId, string messageText)
        {
            if (!await IsPermitted(chatId, UserRole.Admin))
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
                        return;
                    }
                    _logger.LogInformation("Added new room category. Room Category:{Id}, {Name}",roomCategoryResponse.Id,roomCategoryResponse.Name);

                    await _messageSender.SendMessageAsync(chatId, "Категория добавлена");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
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
                        await _messageSender.SendMessageAsync(chatId, "Такой комнаты нет");
                        return;
                    }
                    _logger.LogInformation("Successful receipt of room with Id : {Id}  information.", roomResponse.Id);
                    var statusEmoji = MapToEmojiStatus(roomResponse.Status);
                    var message = $"{statusEmoji} {roomResponse.Name} (id:{roomResponse.Id})\n";
                    message += $"  └ {roomResponse.Description}\n\n";
                    await _messageSender.SendMessageAsync(chatId, message);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to RoomService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
            }
        }
    }
}
