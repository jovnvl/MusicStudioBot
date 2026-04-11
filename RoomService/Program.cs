
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RoomService.BackgroundServices;
using RoomService.Data;
using RoomService.Infrastructure;
using RoomService.Repositories;
using RoomService.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace RoomService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var connectionFactory = new ConnectionFactory() { HostName = "localhost" };
                var connection = await connectionFactory.CreateConnectionAsync();
                var channel = await connection.CreateChannelAsync();

                var builder = WebApplication.CreateBuilder(args);
                builder.Configuration.AddEnvironmentVariables();

                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? Environment.GetEnvironmentVariable("PG_RoomService");


                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("Не настроена строка подключения");
                    return;
                }

                builder.Services.AddScoped<IRoomsService, RoomsService>();
                builder.Services.AddScoped<ICategoryRoomService, CategoryRoomService>();
                builder.Services.AddScoped<IMessageBrokerService, RabbitService>();
                builder.Services.AddScoped<IOutboundMessagesService, OutboundMessagesService>();
                builder.Services.AddScoped<IRoomRepository, PgRoomRepository>();
                builder.Services.AddScoped<ICategoryRoomRepository, PgCategoryRoomRepository>();
                builder.Services.AddScoped<IOutboundMessagesRepository, PgOutboundMessagesRepository>();
                builder.Services.AddHostedService<OutboundMessagesProcessor>();
                builder.Services.AddSingleton<IMessageBroker>(sp => 
                {
                    var rabbitTask = RabbitBroker.CreateAsync("logging_service_queue", connection, channel);
                    return rabbitTask.GetAwaiter().GetResult();
                });
                
                builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(connectionString));
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                builder.Services.AddControllers();
                // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                builder.Services.AddOpenApi();
                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.UseSwagger();
                    app.UseSwaggerUI();

                    //app.UseSwaggerUI(options =>
                    //{
                    //    options.SwaggerEndpoint("/openapi/v1.json", "RoomService API v1");
                    //});
                }

                app.UseHttpsRedirection();
                app.UseAuthorization();
                app.MapControllers();

                //app.MapGet("/", () => "RoomService API is running. Use /swagger for API documentation.");

                app.Lifetime.ApplicationStopped.Register(() =>
                {
                    channel?.CloseAsync();
                    connection?.CloseAsync();
                });

                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                
            }
        }
    }
}
