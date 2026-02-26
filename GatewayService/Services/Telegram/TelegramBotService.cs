using GatewayService.Configuration;
using GatewayService.Models.DTOs;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GatewayService.Services.Telegram
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly ServicesSettings _servicesSettings;
        private const int RegisterCommandPartsCount = 5;
        private const int LoginCommandPartsCount = 2;
        public TelegramBotService(IHttpClientFactory httpClientFactory, ILogger<TelegramBotService> logger, IOptions<TelegramSettings> settings, IOptions<ServicesSettings> servicesSettings) 
        {
            _botClient = new TelegramBotClient(settings.Value.BotToken); ;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _servicesSettings = servicesSettings.Value;
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update == null || update.Message == null || update.Message.Text == null)
                return;
        
            var message = update.Message;
            var chatId = message.Chat.Id;
            var messageText = message.Text;

            _logger.LogInformation("Received message from ChatId: {ChatId}, Text: {Text}", chatId, messageText);

            if (messageText.StartsWith("/start"))
                await HandleStartCommand(chatId);
            else if (messageText.StartsWith("/help"))
                await HandleHelpCommand(chatId);
            else if (messageText.StartsWith("/register"))
                await HandleRegisterCommand(chatId, messageText);
            else if (messageText.StartsWith("/login"))
                await HandleLoginCommand(chatId, messageText);
            else
                await SendMessageAsync(chatId, "Неизвестная команда. Используйте /help");
        }

        private async Task HandleStartCommand(long chatId)
        {
            string welcomeMessage = @"
            Приветствуем в Music Studio Bot! 🎵
   
            Этот бот поможет вам забронировать комнату для репетиций
            ";
            await SendMessageAsync(chatId, welcomeMessage);
            _logger.LogInformation("Sent start command response to ChatId: {ChatId}", chatId);
        }

        private async Task HandleHelpCommand(long chatId)
        {
            string helpMessage = @"
            Доступные команды:
            /start - Начать работу
            /register - Регистрация нового пользователя (формат: /register 'username' 'password' 'firstname' 'lastname')
            /login - Вход в систему (формат: /login 'password')
            /help - Список всех команд
            ";
            await SendMessageAsync(chatId, helpMessage);
            _logger.LogInformation("Sent help command response to ChatId: {ChatId}", chatId);
        }

        private async Task HandleRegisterCommand(long chatId, string messageText)
        {
            string[] registerCommand = messageText.Split(' ');
            if (registerCommand.Length < RegisterCommandPartsCount)
            {
                await SendMessageAsync(chatId,
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
                    await SendMessageAsync(chatId, "Успешная регистрация! Теперь вы можете использовать /login");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await SendMessageAsync(chatId, $"Ошибка регистрации: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
            }
        }

        private async Task HandleLoginCommand(long chatId, string messageText)
        {
            string[] loginCommand = messageText.Split(' ');
            if (loginCommand.Length < LoginCommandPartsCount ) {
                await SendMessageAsync(chatId,
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
                    await SendMessageAsync(chatId, "Успешный вход.");
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    await SendMessageAsync(chatId, $"Ошибка входа: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to IdentityService failed");
                await SendMessageAsync(chatId, "Ошибка соединения с сервером. Попробуйте позже.");
            }
        }


        public async Task SendMessageAsync(long chatId, string text)
        {
            await _botClient.SendMessage(
                chatId: chatId,
                text: text
            );
        }
    }
}
