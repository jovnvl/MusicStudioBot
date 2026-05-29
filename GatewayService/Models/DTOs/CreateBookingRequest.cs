using GatewayService.Models.DTOs;
using GatewayService.Models.Enums;

namespace GatewayService.DTO

{
    public class CreateBookingRequest

    {
        public string Description { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public int RoomId { get; set; }
        public BookingStatus Status { get; set; }
        public BookingPeriod? Period { get; set; }
    }
}
