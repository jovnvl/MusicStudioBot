using NotificationService.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace NotificationService.Models.Entities.DTO
{
    public class BookingDto
    {
        public Guid Id { get; set; }

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Кто бронирует обязателен")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Кабинет для бронирования обязателен")]
        public int RoomId { get; set; }
       
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        public BookingStatus Status { get; set; } = BookingStatus.Booked;

        public DateTime TimeBegin { get; set; }
        public DateTime TimeEnd { get; set; }

    }
}
