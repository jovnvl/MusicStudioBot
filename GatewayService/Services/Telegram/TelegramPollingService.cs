using GatewayService.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GatewayService.Services.Telegram
{
    public class TelegramPollingService : BackgroundService
    {
        private readonly ITelegramBotService _botService;
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        public TelegramPollingService(ITelegramBotService telegramBotService, IOptions<TelegramSettings> settings, ILogger<TelegramBotService> logger)
        {
            _botService = telegramBotService;
            _botClient = new TelegramBotClient(settings.Value.BotToken); ;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken
            );
            _logger.LogInformation("Telegram polling started");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        private async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken)
        {
            await _botService.HandleUpdateAsync(update);
        }
        private Task HandlePollingErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Telegram polling error");
            return Task.CompletedTask;
        }
    }
}
