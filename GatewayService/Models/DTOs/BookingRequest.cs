using GatewayService.Models.Enums;

namespace GatewayService.Models.DTOs
{
    public class BookingRequest
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public int RoomId { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public BookingStatus Status { get; set; }
        public DateTime? TimeBegin { get; set; }
        public DateTime? TimeEnd { get; set; }
    }
}
