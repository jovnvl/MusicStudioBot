using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingService.Models.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        [Required]
        [Column("UserId")]
        public Guid UserId { get; set; }

        [Required]
        [Column("RoomId")]
        public int RoomId { get; set; }

        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public BookingStatus? Status { get; set; } = BookingStatus.NotConfirmed;
        public BookingPeriod Period { get; set; } = new(DateTime.UtcNow, DateTime.UtcNow);
        public string? Description { get; set; }       
    }
}
