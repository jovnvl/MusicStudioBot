using RoomService.Models.Entities;
using RoomService.Services;
namespace RoomService.BackgroundServices

{
    public class OutboundMessagesProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboundMessagesProcessor> _logger;
        private readonly SemaphoreSlim _semaphore = new(3, 3);

        public OutboundMessagesProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboundMessagesProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var outboundMessagesService = scope.ServiceProvider.GetRequiredService<IOutboundMessagesService>();
                    var outboundMessages = await outboundMessagesService.GetActiveOutboundMessagesAsync(ct);
                    
                    if (outboundMessages.Any())
                    {
                        var tasks = new List<Task>();

                        foreach (var message in outboundMessages)
                        {
                            tasks.Add(Task.Run(() => ProcessItemAsync(message, ct)));
                        }

                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task ProcessItemAsync(OutboundMessages outboundMessage, CancellationToken ct)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var outboundMessagesService = scope.ServiceProvider.GetRequiredService<IOutboundMessagesService>();
                var messageBrokerService = scope.ServiceProvider.GetRequiredService<IMessageBrokerService>();

                await _semaphore.WaitAsync();

                try
                {
                    await messageBrokerService.SendMessageToLogAsync(outboundMessage.Level, outboundMessage.Message, outboundMessage.EventType, ct);
                    await outboundMessagesService.UpdateOutboundMessageAsync(outboundMessage.Id, MessageStatus.Done, ct);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Warning, ex.Message);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
    }
}
