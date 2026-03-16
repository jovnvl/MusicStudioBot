using RoomService.DTO;
using RoomService.Infrastructure;
using RoomService.Models.Entities;

namespace RoomService.Services
{
    public class RabbitService : IMessageBrokerService
    {
        private readonly IMessageBroker _messageBroker;
        
        public RabbitService(IMessageBroker messageQueue)
        {
            _messageBroker = messageQueue;
        }
        
        public async Task SendMessageToLogAsync(LogLevel logLevel, string message, string eventType, CancellationToken ct) 
        {
            await _messageBroker.PublishMessageAsync<LogEventDto>(new LogEventDto(logLevel.ToString(), eventType, message), "logging_service_queue", ct);
        }
    }
}
