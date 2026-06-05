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
    public class IdentityCommandHandler : CommandHandler
    {
        private readonly ServicesSettings _servicesSettings;
        private readonly IConversationStateService _conversationService;
        private const int RegisterCommandPartsCount = 4;
        private const int LoginCommandPartsCount = 2;
        private const int UpdateCommandMinPartsCount = 2;
        private const int ChangeUserRoleCommandPartsCount = 3;

        public IdentityCommandHandler(
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

                    await _sessionService.SaveTokenAsync(
                        chatId,
                        authResponse.Token,
                        authResponse.RefreshToken
                    );

                    _logger.LogInformation("Registration successful. New user id: {UserId}", authResponse.UserId);
                    await LogToServiceAsync("Information", "user-registered", $"Registration successful. New user id: {authResponse.UserId}");
                    await _messageSender.SendMessageAsync(chatId, "Успешная регистрация!");
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

        public async Task HandleRegistrationPasswordInput(long chatId, string password, UserConversationData conversation)
        {
            if (password.Length < 4)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "❌ Пароль должен содержать минимум 4 символа. Попробуйте снова:");
                return;
            }

            conversation.SetValue("password", password);
            conversation.State = ConversationState.AwaitingRegistrationFirstName;

            await _messageSender.SendMessageAsync(chatId,
                "Введите ваше имя:",
                replyMarkup: KeyboardHelper.GetCancelKeyboard());
        }

        public async Task HandleRegistrationFirstNameInput(long chatId, string firstName, UserConversationData conversation)
        {
            conversation.SetValue("firstName", firstName);
            conversation.State = ConversationState.AwaitingRegistrationLastName;

            await _messageSender.SendMessageAsync(chatId,
                "Введите вашу фамилию:",
                replyMarkup: KeyboardHelper.GetCancelKeyboard());
        }

        public async Task HandleRegistrationLastNameInput(long chatId, string lastName, UserConversationData conversation)
        {
            var password = conversation.GetValue<string>("password");
            var firstName = conversation.GetValue<string>("firstName");

            var registerRequest = new RegisterRequest
            {
                Username = chatId.ToString(), // Используем chatId как username
                Password = password!,
                FirstName = firstName,
                LastName = lastName,
                TelegramId = chatId
            };

            try
            {
                var response = await SendRequestAsync(HttpMethod.Post,
                    $"{_servicesSettings.IdentityServiceUrl}/api/auth/register",
                    chatId,
                    registerRequest);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(body);

                    if (authResponse != null)
                    {
                        await _sessionService.SaveTokenAsync(
                            chatId,
                            authResponse.Token,
                            authResponse.RefreshToken
                        );
                    }

                    await _messageSender.SendMessageAsync(chatId,
                        "✅ Регистрация успешна!\n\nТеперь используйте /login password для входа.");
                    await LogToServiceAsync("Information", "user-registered",
                        $"New user registered: {chatId}");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId,
                        $"❌ Ошибка регистрации: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером.");
            }
            finally
            {
                conversation.Clear();
            }
        }

        public async Task HandleUpdateProfileInput(long chatId, string input, UserConversationData conversation)
        {
            if (!await IsPermitted(chatId, UserRole.Student))
            {
                conversation.Clear();
                return;
            }

            var parts = input.Split(' ');
            if (parts.Length < 3)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "❌ Неверный формат!\n\nИспользуйте: username firstname lastname\nПример: new_user - NewLastName");
                return;
            }

            var updateProfileRequest = new UpdateProfileRequest
            {
                Username = parts[0] != "-" ? parts[0] : null,
                FirstName = parts[1] != "-" ? parts[1] : null,
                LastName = parts[2] != "-" ? parts[2] : null
            };

            if (updateProfileRequest.Username == null &&
                updateProfileRequest.FirstName == null &&
                updateProfileRequest.LastName == null)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "❌ Укажите хотя бы один параметр для обновления!");
                return;
            }

            try
            {
                var response = await SendRequestAsync(HttpMethod.Put,
                    $"{_servicesSettings.IdentityServiceUrl}/api/auth/user/update_profile",
                    chatId,
                    updateProfileRequest);

                if (response.IsSuccessStatusCode)
                {
                    await _messageSender.SendMessageAsync(chatId, "✅ Профиль обновлен!");
                    await LogToServiceAsync("Information", "profile-updated",
                        $"Profile updated for chatId: {chatId}");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await _messageSender.SendMessageAsync(chatId,
                        $"❌ Ошибка: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile update failed");
                await _messageSender.SendMessageAsync(chatId, "Ошибка соединения с сервером.");
            }
            finally
            {
                conversation.Clear();
            }
        }
    }
}
