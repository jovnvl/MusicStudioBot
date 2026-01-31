
namespace RoomService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

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
