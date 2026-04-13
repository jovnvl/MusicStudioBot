using GatewayService.Models.Enums;
using Telegram.Bot.Types;

namespace GatewayService.Models.DTOs
{
    public class BookingResponse
    {
        public Guid Id { get; set; }
       
        public Guid UserId { get; set; }
        
        public User User { get; set; } = null!;

        public int RoomId { get; set; }
        //public Room Room { get; set; } = null!;

        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public BookingStatus? Status { get; set; } = BookingStatus.NotConfirmed;
        public DateTime? TimeBegin { get; set; }
        public DateTime? TimeEnd { get; set; }
        public string? Description { get; set; }
    }
}
