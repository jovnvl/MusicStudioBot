using NotificationService.Models.Entities.DTO;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationService.Services
{
    public class NotificationConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public NotificationConsumerService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            string queueName = "notification_service_queue";
            await channel.QueueDeclareAsync(queue: queueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            Console.WriteLine(" [*] Ожидание сообщений...");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($" [x] Получено: {message}");

                using var scope = _scopeFactory.CreateScope();
                var notiferService = scope.ServiceProvider.GetRequiredService<INotiferService>();

                var notificationDto = JsonSerializer.Deserialize<NotificationDto>(body);

                if (notificationDto != null)
                {
                    await notiferService.UpsertNotiferAsync(notificationDto, ct);
                }
            };
            await channel.BasicConsumeAsync(queue: queueName,
                     autoAck: true,
                     consumer: consumer);
            Console.WriteLine(" Нажмите [enter] для выхода.");
            Console.ReadLine();
        }
    }
}
