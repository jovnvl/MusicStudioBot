using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace LoggingService
{
    internal class Program
    {
        static async Task Main(string[] args)
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
}
