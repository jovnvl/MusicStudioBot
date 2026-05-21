using BookingService.Services;

namespace BookingService.Exceptions
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                context.Response.StatusCode = 499;
                await context.Response.WriteAsJsonAsync(new { Message = "Request cancelled" });
            }
            catch (Exception ex)
            {
                var endpoint = context.GetEndpoint()?.DisplayName ?? "Unknown";
                var httpMethod = context.Request.Method;
                var path = context.Request.Path;
                var eventType = $"{httpMethod}_{path.ToString().Replace("/", "_")}".ToLower();

                using (var scope = context.RequestServices.CreateScope()) 
                {
                    var outboundService = scope.ServiceProvider.GetRequiredService<IOutboundMessagesService>();
                    await outboundService.CreateOutboundMessageToLogAsync(
                    LogLevel.Critical,
                    $"[{httpMethod}] {path}: {ex.Message}",
                    eventType, true,
                    CancellationToken.None);
                }

                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", httpMethod, path);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { Message = ex.Message });
            }
        }
    }
}