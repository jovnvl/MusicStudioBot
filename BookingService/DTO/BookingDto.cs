using BookingService.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace BookingService.DTO

{
    public class BookingDto
    {
        [Display(Name = "Идентификатор")]
        public Guid Id { get; init; }

        [StringLength(500,
        ErrorMessage = "Описание не должно превышать 500 символов")]
        [Display(Name = "Описание")]
        public string Description { get; init; } = string.Empty;

        [Display(Name = "Пользователь")]
        [Required(ErrorMessage = "Кто бронирует обязателен")]
        public Guid UserId { get; init; } = Guid.Parse("284616f4-e14d-4f05-af9b-baef54d9a6eb");

        [Display(Name = "Кабинет")]
        [Required(ErrorMessage = "Кабинет для бронирования обязателен")]
        public int RoomId { get; init; } = 1;
       
        [Display(Name = "Дата создания")]
        public DateTime CreationDate { get; init; } = DateTime.UtcNow;

        [Range(0, 4,
            ErrorMessage = "Статус бронирования кабинета должен быть от 0 до 3 (0-не подтвержден, 1-отменен, 2-забронирован, 3-завершен)")]
        [Display(Name = "Статус бронирования")]
        public BookingStatus Status { get; init; } = BookingStatus.Booked;

        [Display(Name = "Дата и время начала бронирования")]
        public DateTime? TimeBegin { get; init; } = DateTime.UtcNow;
        [Display(Name = "Дата и время окончания бронирования")]
        public DateTime? TimeEnd { get; init; } = DateTime.UtcNow.AddMinutes(45);

    }
}
