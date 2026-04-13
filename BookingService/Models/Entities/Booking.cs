using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
        public DateTime? TimeBegin { get; set; }
        public DateTime? TimeEnd { get; set; }
        public string? Description { get; set; }       
    }
}
