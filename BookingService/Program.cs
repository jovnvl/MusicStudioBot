using BookingService.Data;
using BookingService.Domain.Events;
using BookingService.Domain.Handlers;
using BookingService.Infrastructure;
using BookingService.Infrastructure.Concurrency;
using BookingService.Infrastructure.Events;
using BookingService.Infrastructure.MessageBroker;
using BookingService.Repositories;
using BookingService.Services;
using BookingService.Common;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using System.Text;

namespace BookingService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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
                        ClockSkew = TimeSpan.Zero,
                        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                        ValidAudience = builder.Configuration["JwtSettings:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!))
                    };
                });
            // ===== JWT AUTHENTICATION =====

            builder.Services.AddAuthorization();

            builder.Configuration.AddEnvironmentVariables();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("PG_BookingService");


            builder.Services.AddScoped<IBookingService, BookingService.Services.BookingService>();
            builder.Services.AddScoped<IBookingRepository, BookingRepository>();
            //RabbitMQ
            //Простой способ публикации сообщений в RabbitMQ - создавать новое соединение и канал для каждого сообщения. Это просто, но неэффективно из-за накладных расходов на установление соединения.
            //builder.Services.AddSingleton<RabbitMqConnection>();
            //builder.Services.AddSingleton<IMessagePublisher, RabbitMQAdapter>();

            //для публикации с помощью пула каналов RabbitMQ
            //    пул каналов, то схема регистрации обычно такая:
            //1.ConnectionFactory(Singleton)
            builder.Services.AddSingleton(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();

                return new ConnectionFactory
                {
                    HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                };
            });
            //2.RabbitMqChannelPool(Singleton)
            builder.Services.AddSingleton<RabbitMqChannelPool>();
            //3.RabbitMQAdapterPool(Singleton)
            builder.Services.AddSingleton<IMessagePublisher, RabbitMQAdapterPool>();
            /*Kafka
            builder.Services.AddSingleton<
                IProducer<string, string>>(
                _ =>
                {
                    var config = new ProducerConfig
                    {
                        BootstrapServers =
                            builder.Configuration["Kafka:BootstrapServers"]
                    };

                    return new ProducerBuilder<string, string>(
                        config)
                        .Build();
                });
            builder.Services.AddSingleton<
                IMessagePublisher,
                KafkaAdapter>();
            //если не нужен отдельный класс-держатель для продюсера:
            builder.Services.AddSingleton<KafkaProducerHolder>();
            */
            //Add Domain Events
            //одна открытая generic-регистрация
            builder.Services.AddScoped(typeof(IEventDispatcher<>), typeof(EventDispatcher<>));
            //и конкретные обработчики событий
            builder.Services.AddScoped<
                IEventHandler<BookingEvent>,
                BookingEventHandler>();
            
            builder.Services.AddScoped<
                IEventHandler<StatisticEvent>,
                StatisticEventHandler>();
            //
            //Add BookingValidationStrategy
            builder.Services.AddScoped<
                IBookingValidationStrategy,
                DateValidationStrategy>();

            //builder.Services.AddScoped<
            //    IBookingValidationStrategy,
            //    FutureBookingValidationStrategy>();

            builder.Services.AddScoped<
                IBookingValidationStrategy,
                OverlapValidationStrategy>();

            builder.Services.AddScoped<
                IBookingValidationPipeline,
                BookingValidationPipeline>();
            //add BookingLockProvider
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<
                IBookingLockProvider,
                BookingLockProvider>();
            //
            builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                     //policy.WithOrigins());
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());//Безопасность под угрозой - Очень открытый CORS
            });

            var app = builder.Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Application started");

            if (string.IsNullOrEmpty(connectionString))
            {
                //Console.WriteLine("Не настроена строка подключения");
                app.Logger.LogInformation("Не настроена строка подключения");
                return;
            }

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.MapControllers();

            //app.MapGet("/", () => "BookingService API is running. Use /swagger for API documentation.");

            app.Run();
        }
    }
}
