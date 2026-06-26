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

        public IdentityCommandHandler(
            IHttpClientFactory httpClientFactory, 
            ILogger<CommandHandler> logger, 
            IUserSessionService sessionService, 
            IMessageSender messageSender, 
            IRabbitMQPublisher rabbitMQPublisher,
            IOptions<ServicesSettings> servicesSettings) : base(httpClientFactory, logger, sessionService, messageSender, rabbitMQPublisher)
        {
            _servicesSettings = servicesSettings.Value;
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
            var firstName = conversation.GetValue<string>("firstName");

            var registerRequest = new RegisterRequest
            {
                Username = conversation.GetValue<string>("username") ?? chatId.ToString(),
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
                        "✅ Регистрация успешна!\n\nТеперь используйте /start для входа.");
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
            if (parts.Length < 2)
            {
                await _messageSender.SendMessageAsync(chatId,
                    "❌ Неверный формат!\n\nИспользуйте: firstname lastname\nПример: NewFirstName -");
                return;
            }

            var updateProfileRequest = new UpdateProfileRequest
            {
                FirstName = parts[0] != "-" ? parts[0] : null,
                LastName = parts[1] != "-" ? parts[1] : null
            };

            if (updateProfileRequest.FirstName == null &&
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
