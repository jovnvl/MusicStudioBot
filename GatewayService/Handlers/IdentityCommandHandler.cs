using GatewayService.Configuration;
using GatewayService.Models.DTOs;
using GatewayService.Models.Enums;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GatewayService.Handlers
{
    public class IdentityCommandHandler : CommandHandler
    {
        private readonly ServicesSettings _servicesSettings;
        private const int RegisterCommandPartsCount = 4;
        private const int LoginCommandPartsCount = 2;
        private const int UpdateCommandMinPartsCount = 2;
        private const int ChangeUserRoleCommandPartsCount = 3;

        public IdentityCommandHandler(IHttpClientFactory httpClientFactory, ILogger<CommandHandler> logger, IUserSessionService sessionService, IMessageSender messageSender, IRabbitMQPublisher rabbitMQPublisher, IOptions<ServicesSettings> servicesSettings) : base(httpClientFactory, logger, sessionService, messageSender, rabbitMQPublisher)
        {
            _servicesSettings = servicesSettings.Value;
        }

        public async Task HandleRegisterCommand(long chatId, string chatUsername, string messageText)
        {
            string[] registerCommand = messageText.Split(' ');
            if (registerCommand.Length < RegisterCommandPartsCount)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "Неверный формат команды!\nИспользуйте: /register password firstName lastName");
                return;
            }
            string username = chatUsername;
            string password = registerCommand[1];
            string firstName = registerCommand[2];
            string lastName = registerCommand[3];
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
                var response = await SendRequestAsync(HttpMethod.Post, $"{_servicesSettings.IdentityServiceUrl}/api/auth/register", chatId, registerRequest);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(body);
                    if (authResponse == null)
                    {
                        _logger.LogError("Failed to deserialize AuthResponse");
                        await LogToServiceAsync("Error", "auth-response-fail", "Failed to deserialize AuthResponse");
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
                await LogToServiceAsync("Error", "http-request-fail", "HTTP request to IdentityService failed");
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

            var loginRequest = new LoginRequest
            {
                Password = loginCommand[1],
                TelegramId = chatId,
            };

            try
            {
                var response = await SendRequestAsync(HttpMethod.Post, $"{_servicesSettings.IdentityServiceUrl}/api/auth/login", chatId, loginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(body);
                    if (authResponse == null)
                    {
                        _logger.LogError("Failed to deserialize AuthResponse");
                        await LogToServiceAsync("Error", "auth-response-fail", "Failed to deserialize AuthResponse");
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
                await LogToServiceAsync("Error", "request-fail", "HTTP request to IdentityService failed");
            }
        }

        public async Task HandleMyProfileCommand(long chatId)
        {
            if (!await IsPermitted(chatId, UserRole.Student))
                return;

            try
            {
                var response = await SendRequestAsync(HttpMethod.Get, $"{_servicesSettings.IdentityServiceUrl}/api/auth/user/telegram/{chatId}", chatId);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var userResponse = JsonSerializer.Deserialize<UserResponse>(body);
                    if (userResponse == null)
                    {
                        _logger.LogError("Failed to deserialize UserResponse");
                        await LogToServiceAsync("Error", "user-response-fail", "Failed to deserialize UserResponse");
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
                    await LogToServiceAsync("Error", "get-user-info-fail", $"Receipt of user information failed: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-fail", "HTTP request to IdentityService failed");
            }
        }

        public async Task HandleUpdateProfileCommand(long chatId, string messageText)
        {

            if (!await IsPermitted(chatId, UserRole.Student))
                return;

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


            try
            {
                var response = await SendRequestAsync(HttpMethod.Put, $"{_servicesSettings.IdentityServiceUrl}/api/auth/user/update_profile", chatId, updateProfileRequest);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var userResponse = JsonSerializer.Deserialize<UserResponse>(body);
                    if (userResponse == null)
                    {
                        _logger.LogError("Failed to deserialize UserResponse");
                        await LogToServiceAsync("Error", "user-response-fail", "Failed to deserialize UserResponse");
                        return;
                    }
                    _logger.LogInformation("Successful update of user information. User: {Id}, {Username}", userResponse.Id, userResponse.Username);
                    await LogToServiceAsync("Information", "user-update", $"Successful update of user information. User: {userResponse.Id}, {userResponse.Username}");
                    await _messageSender.SendMessageAsync(chatId, $"Id: {userResponse.Id}\nЛогин: {userResponse.Username}\nИмя: {userResponse.FirstName}\nФамилия: {userResponse.LastName}");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "user-update-fail", $"Update of user information failed: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-fail", "HTTP request to IdentityService failed");
            }
        }
        public async Task HandleGetUsersCommand(long chatId)
        {
            if (!await IsPermitted(chatId, UserRole.Moderator))
                return;
            try
            {
                var response = await SendRequestAsync(HttpMethod.Get, $"{_servicesSettings.IdentityServiceUrl}/api/auth/user/all", chatId);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<UserResponse>>(body);
                    if (users == null || users.Count == 0)
                    {
                        _logger.LogError("Failed to deserialize UserResponse");
                        await LogToServiceAsync("Error", "user-response-fail", "Failed to deserialize UserResponse");
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
                    await LogToServiceAsync("Error", "get-users-fail", $"Failed to get users info: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-fail", "HTTP request to IdentityService failed");
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

            var changeRoleRequest = new ChangeRoleRequest
            {
                Id = id,
                Role = role,
            };

            try
            {
                var response = await SendRequestAsync(HttpMethod.Put, $"{_servicesSettings.IdentityServiceUrl}/api/auth/user/change_role", chatId, changeRoleRequest);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var userResponse = JsonSerializer.Deserialize<UserResponse>(body);
                    if (userResponse == null)
                    {
                        _logger.LogError("Failed to deserialize UserResponse");
                        await LogToServiceAsync("Error", "user-response-fail", "Failed to deserialize UserResponse");
                        await _messageSender.SendMessageAsync(chatId, "Такого пользователя нет");
                        return;
                    }
                    _logger.LogInformation("User {Id} role successfully changed to {Role}.", userResponse.Id, userResponse.Role);
                    await LogToServiceAsync("Information", "change-role", $"User {userResponse.Id} role successfully changed to {userResponse.Role}.");
                    var message = "Роль пользователя изменена";
                    await _messageSender.SendMessageAsync(chatId, message);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId, $"Ошибка получения данных: {errorMessage}");
                    await LogToServiceAsync("Error", "change-role-fail", $"Failed to change user role: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
                await LogToServiceAsync("Error", "request-fail", "HTTP request to IdentityService failed");
            }
        }
    }
}
