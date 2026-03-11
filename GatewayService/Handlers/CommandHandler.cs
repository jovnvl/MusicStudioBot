using GatewayService.Configuration;
using GatewayService.Models.DTOs;
using GatewayService.Services;
using Microsoft.Extensions.Options;
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
        private const int RegisterCommandPartsCount = 5;
        private const int LoginCommandPartsCount = 2;
        private const int UpdateCommandMinPartsCount = 2;

        public CommandHandler(IHttpClientFactory httpClientFactory, ILogger<CommandHandler> logger, IOptions<ServicesSettings> servicesSettings, IUserSessionService sessionService, IMessageSender messageSender)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _servicesSettings = servicesSettings.Value;
            _sessionService = sessionService;
            _messageSender = messageSender;
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
            string helpMessage = @"
            Доступные команды:
            /start - Начать работу
            /register - Регистрация нового пользователя (формат: /register 'username' 'password' 'firstname' 'lastname')
            /login - Вход в систему (формат: /login 'password')
            /help - Список всех команд
            /myprofile - Получить данные профиля
            /update_profile - Изменить данные профиля (формат: /update_profile [username] [firstname] [lastname]; для пропуска параметра ставить символ -)
            ";
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
    }
}
