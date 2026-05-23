using Microsoft.Extensions.Hosting;
using BookingService.Services;
namespace BookingService.BackgroundServices

{
    public class OutboundMessagesProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OutboundMessagesProcessor(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var outboundMessagesService = scope.ServiceProvider.GetRequiredService<IOutboundMessagesService>();
                    //var messageBrokerService = scope.ServiceProvider.GetRequiredService<IMessageBrokerService>();
                    var outboundMessage = await outboundMessagesService.GetActiveOutboundMessagesAsync(ct);
                    if (outboundMessage != null)
                    {
                        try
                        {
                            //await messageBrokerService.SendMessageToLogAsync(outboundMessage.Level, outboundMessage.Message, outboundMessage.EventType, ct);
                            await outboundMessagesService.UpdateOutboundMessageAsync(outboundMessage.Id, MessageStatus.Done, ct);
                        }
                        catch
                        {
                            throw;
                        }
                    }
                }

                await Task.Delay(2000, ct).ConfigureAwait(false);
            }
        }
    }
}
