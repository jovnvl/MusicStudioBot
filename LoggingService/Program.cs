
using LoggingService.Data;
using LoggingService.Repositories;
using LoggingService.Services;
using Microsoft.EntityFrameworkCore;

namespace LoggingService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("PG_LoggingService");

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Не настроена строка подключения");
                return;
            }

            builder.Services.AddControllers();
            builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddScoped<ILogService, LogService>();
            builder.Services.AddScoped<IRepository, PgLogsRepository>();
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHostedService<LogConsumerService>();

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
