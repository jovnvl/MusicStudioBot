using GatewayService.Configuration;
using GatewayService.Models.DTOs;
using Microsoft.Extensions.Options;

namespace GatewayService.Services.Notifications
{
    public class BookingNotificationHandler
    {
        private readonly IMessageSender _messageSender;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BookingNotificationHandler> _logger;
        private readonly ServicesSettings _servicesSettings;

        public BookingNotificationHandler(
            IMessageSender messageSender,
            IHttpClientFactory httpClientFactory,
            ILogger<BookingNotificationHandler> logger,
            IOptions<ServicesSettings> servicesSettings)
        {
            _messageSender = messageSender;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _servicesSettings = servicesSettings.Value;
        }

        public async Task HandleAsync(BookingNotificationMessage message)
        {
            // обработка события RabbitMQ
            /*
            var userNames =
    await GetUserNamesAsync(
        new[] { message.UserId });

            var roomNames =
                await GetRoomNamesAsync(
                    new[] { message.RoomId });

            var booking = MapToBookingResponse(message);

            var text =
                BuildNotificationText(
                    message.EventType,
                    booking,
                    userNames,
                    roomNames);

            await NotifyOwnerAsync(
                message.UserId,
                text);

            await NotifyModeratorsAsync(
                text);
            */
        }

        private async Task NotifyOwnerAsync()
        {
        }

        private async Task NotifyModeratorsAsync()
        {
        }
    }
}
