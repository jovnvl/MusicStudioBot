
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using RoomService.Data;
using RoomService.Repositories;
using RoomService.Services;

namespace RoomService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddScoped<IRoomsService, RoomsService>();
            builder.Services.AddScoped<ICategoryRoomService, CategoryRoomService>();
            //builder.Services.AddScoped<IRoomRepository, InMemoryRoomRepository>();
            //builder.Services.AddScoped<ICategoryRoomRepository, InMemoryCategoryRoomRepository>();
            builder.Services.AddScoped<IRoomRepository, PgRoomRepository>();
            builder.Services.AddScoped<ICategoryRoomRepository, PgCategoryRoomRepository>();
            builder.Services.AddDbContext<DataContext>();
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
                //    options.SwaggerEndpoint("/openapi/v1.json", "RoomService API v1");
                //});
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            //app.MapGet("/", () => "RoomService API is running. Use /swagger for API documentation.");

            app.Run();
        }
    }
}
