using GatewayService.Services.Notifications;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using GatewayService.Models.DTOs;

namespace GatewayService.Services.RabbitMQ
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQConsumer> _logger;

        public RabbitMQConsumer(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<RabbitMQConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:Host"] ?? "localhost"
            };

            var connection = await factory.CreateConnectionAsync(stoppingToken);
            var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: "notification_service_queue",
                durable: false,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.Span);

                    var dto = JsonSerializer.Deserialize<BookingNotificationDto>(json);

                    if (dto != null)
                    {
                        using var scope = _serviceProvider.CreateScope();

                        var handler =
                            scope.ServiceProvider.GetRequiredService<BookingNotificationHandler>();

                        await handler.HandleAsync(dto);
                    }

                    await channel.BasicAckAsync(
                        ea.DeliveryTag,
                        false,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification");
                }
            };

            await channel.BasicConsumeAsync(
                queue: "notification_service_queue",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
