using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LoggingService.Services
{
    public class LogConsumerService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            string queueName = "logging_service_queue";
            // Объявляем ту же очередь (на всякий случай)
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
            };
            await channel.BasicConsumeAsync(queue: queueName,
                     autoAck: true,
                     consumer: consumer);
            Console.WriteLine(" Нажмите [enter] для выхода.");
            Console.ReadLine();
        }
    }
    //{
    //    private readonly IChannel _channel;
    //    private readonly IServiceScopeFactory _scopeFactory;

    //    protected override async Task ExecuteAsync(CancellationToken ct)
    //    {
    //        var consumer = new AsyncEventingBasicConsumer(_channel);

    //        consumer.ReceivedAsync += async (_, ea) =>
    //        {
    //            var body = ea.Body.ToArray();
    //            var log = JsonSerializer.Deserialize<LogEntry>(body);

    //            using var scope = _scopeFactory.CreateScope();
    //            var repository = scope.ServiceProvider.GetRequiredService<ILogRepository>();

    //            // Сохраняем в БД
    //            await repository.AddAsync(log, ct);

    //            // Подтверждаем получение
    //            await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
    //        };

    //        await _channel.BasicConsumeAsync("logs-queue", false, consumer, ct);
    //    }
    //}
}
