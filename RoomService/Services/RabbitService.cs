using RoomService.DTO;
using RoomService.Infrastructure;
using RoomService.Models.Entities;

namespace RoomService.Services
{
    public class RabbitService : IMessageBrokerService
    {
        private readonly IMessageBroker _messageBroker;
        
        public RabbitService(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }
        
        public async Task SendMessageToLogAsync(string logLevel, string message, string eventType, CancellationToken ct) 
        {
            await _messageBroker.PublishMessageAsync<LogEventDto>(new LogEventDto(logLevel, eventType, message), "logging_service_queue", ct);
        }
    }
}
