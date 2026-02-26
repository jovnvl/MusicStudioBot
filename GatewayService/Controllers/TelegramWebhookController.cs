using GatewayService.Services.Telegram;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace GatewayService.Controllers
{
    [ApiController]
    [Route("api/telegram")]
    public class TelegramWebhookController : ControllerBase
    {
        private readonly ITelegramBotService _telegramBotService;
        private readonly ILogger<TelegramWebhookController> _logger;

        public TelegramWebhookController(ITelegramBotService telegramBotService, ILogger<TelegramWebhookController> logger)
        {
            _telegramBotService = telegramBotService;
            _logger = logger;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            try
            {
                await _telegramBotService.HandleUpdateAsync(update);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return Ok(); // всегда возвращаем 200, чтобы Telegram не повторял запрос
            }
        }
    }
}
