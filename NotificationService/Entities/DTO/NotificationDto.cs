using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models.Entities.DTO
{

    public class NotificationDto
    {
        public DateTime Timestamp { get; set; }

        [Required]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public BookingDto BookingDto { get; set; } = new BookingDto();
    }
}
