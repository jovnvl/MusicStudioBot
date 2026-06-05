using GatewayService.Configuration;
using GatewayService.Handlers;
using GatewayService.Middleware;
using GatewayService.Services;
using GatewayService.Services.RabbitMQ;
using GatewayService.Services.Telegram;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("Telegram"));
builder.Services.Configure<ServicesSettings>(builder.Configuration.GetSection("Services"));

builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(configuration!);
});

builder.Services.AddSingleton<IUserSessionService, UserSessionService>();
builder.Services.AddSingleton<IConversationStateService, ConversationStateService>();

builder.Services.AddSingleton<ITelegramBotService, TelegramBotService>();
builder.Services.AddSingleton<IMessageSender, MessageSenderService>();
builder.Services.AddHostedService<TelegramPollingService>();

builder.Services.AddSingleton<IRabbitMQPublisher, RabbitMQPublisher>();

builder.Services.AddScoped<SystemCommandHandler>();
builder.Services.AddScoped<IdentityCommandHandler>();
builder.Services.AddScoped<RoomCommandHandler>();
builder.Services.AddScoped<BookingCommandHandler>();
builder.Services.AddScoped<TelegramAuthMiddleware>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();

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
