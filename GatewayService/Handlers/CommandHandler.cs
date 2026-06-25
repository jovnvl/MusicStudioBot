using GatewayService.DTO;
using GatewayService.Models.DTOs;
using GatewayService.Models.Enums;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace GatewayService.Handlers
{
    public abstract class CommandHandler
    {
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly ILogger<CommandHandler> _logger;
        protected readonly IUserSessionService _sessionService;
        protected readonly IMessageSender _messageSender;
        protected readonly IRabbitMQPublisher _rabbitMQPublisher;

        public CommandHandler(
            IHttpClientFactory httpClientFactory, 
            ILogger<CommandHandler> logger, 
            IUserSessionService sessionService, 
            IMessageSender messageSender, 
            IRabbitMQPublisher rabbitMQPublisher
            )
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _sessionService = sessionService;
            _messageSender = messageSender;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        protected async Task<bool> IsPermitted(long chatId, UserRole role)
        {
            var token = await _sessionService.GetTokensAsync(chatId);
            if (string.IsNullOrEmpty(token.accessToken))
            {
                await _messageSender.SendMessageAsync(chatId, "Вы не авторизованы.");
                return false;
            }
            if (!HasRole(token.accessToken, role))
            {
                await _messageSender.SendMessageAsync(chatId, "Недостаточно прав.");
                return false;
            }
            return true;
        }

        protected bool HasRole(string token, UserRole requiredRole)
        {
            var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;
            var tokenRole = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (!Enum.TryParse<UserRole>(tokenRole, out var userRole))
                return false;
            return (int)userRole >= (int)requiredRole;
        }

        protected async Task LogToServiceAsync(string level, string eventType, string message)
        {
            await _rabbitMQPublisher.PublishAsync("logging_service_queue", new LogEventDto(level, eventType, message));
        }
        protected async Task NotificationToServiceAsync(string level, string eventType, BookingRequest message)
        {
            await _rabbitMQPublisher.PublishAsync("notification_service_queue", new BookingNotificationDto(eventType, message));
        }

        protected async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string endpoint, long chatId, object? request = null)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var token = await _sessionService.GetTokensAsync(chatId);
            if (!string.IsNullOrEmpty(token.accessToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.accessToken);
            }         
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
    }
}
