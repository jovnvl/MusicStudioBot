using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Services;

namespace NotificationService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("PG_NotificationService");

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Не настроена строка подключения");
                return;
            }

            builder.Services.AddControllers();
            builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddScoped<INotiferService, NotiferService>();
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHostedService<BookingReminderDispatcher>();
            builder.Services.AddHostedService<NotificationConsumerService>();
            // DbContext + обработчики events как настроено

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
