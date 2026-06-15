
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using RoomService.BackgroundServices;
using RoomService.Data;
using RoomService.Exceptions;
using RoomService.Infrastructure;
using RoomService.Repositories;
using RoomService.Services;
using System.Text;

namespace RoomService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);
                var rabbitConnectionString = builder.Configuration.GetConnectionString("RabbitMQConnection") ?? "localhost";
                var connectionFactory = new ConnectionFactory() { HostName =  rabbitConnectionString};
                var connection = await connectionFactory.CreateConnectionAsync();
                var channel = await connection.CreateChannelAsync();

                // ===== JWT AUTHENTICATION =====
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                            ValidAudience = builder.Configuration["JwtSettings:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!))
                        };
                    });
                // ===== JWT AUTHENTICATION =====

                builder.Services.AddAuthorization();

                builder.Services.AddControllers();

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

                var broker = await RabbitBroker.CreateAsync("logging_service_queue", connection, channel);
                builder.Services.AddSingleton<IMessageBroker>(broker);
                builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(connectionString));
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                builder.Services.AddControllers();
                builder.Services.AddOpenApi();

                builder.Services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
                });

                var app = builder.Build();
                app.UseMiddleware<GlobalExceptionMiddleware>();
                app.UseCors();
                app.UseAuthentication(); 
                app.UseAuthorization();

                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();
                app.UseAuthorization();
                app.MapControllers();
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
