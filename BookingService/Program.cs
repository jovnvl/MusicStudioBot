
/*
namespace BookingService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
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
*/
using BookingService.Data;
using BookingService.Repositories;
using BookingService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace BookingService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("PG_BookingService");


            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Не настроена строка подключения");
                return;
            }

            builder.Services.AddScoped<IAuthService, BookingService.Services.AuthService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services.AddScoped<IBookingService, BookingService.Services.BookingService>();
            builder.Services.AddScoped<IBookingRepository, BookingRepository>();

            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<ICategoryRoomService, CategoryRoomService>();
            ////builder.Services.AddScoped<IRoomRepository, MemoryRoomRepository>();
            ////builder.Services.AddScoped<ICategoryRoomRepository, MemoryCategoryRoomRepository>();
            builder.Services.AddScoped<IRoomRepository, RoomRepository>();
            builder.Services.AddScoped<ICategoryRoomRepository, CategoryRoomRepository>();
            builder.Services.AddDbContext<DataContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add services to the container.

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
                //    options.SwaggerEndpoint("/openapi/v1.json", "BookingService API v1");
                //});
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            //app.MapGet("/", () => "BookingService API is running. Use /swagger for API documentation.");

            app.Run();
        }
    }
}
