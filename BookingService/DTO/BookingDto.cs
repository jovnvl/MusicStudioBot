using BookingService.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace BookingService.DTO

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

        [Range(0, 4,
            ErrorMessage = "Статус бронирования кабинета должен быть от 0 до 3 (0-не подтвержден, 1-отменен, 2-забронирован, 3-завершен)")]
        public BookingStatus Status { get; set; } = BookingStatus.Booked;

        public DateTime? TimeBegin { get; set; } //= DateTime.UtcNow;
        public DateTime? TimeEnd { get; set; } //= DateTime.UtcNow.AddMinutes(45);

    }
}
