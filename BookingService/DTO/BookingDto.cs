using BookingService.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace BookingService.DTO

{
    public class BookingDto
    {
        [Display(Name = "Id бронирования кабинета")]
        [Required(ErrorMessage = "ID бронирования кабинета обязателен")]
        public int Id { get; init; }

        [StringLength(500,
        ErrorMessage = "Описание не должно превышать 500 символов")]
        [Display(Name = "Описание")]
        public string Description { get; init; } = string.Empty;

        [Display(Name = "Пользователь")]
        [Required(ErrorMessage = "Кто бронирует обязателен")]
        public User User { get; init; } 

        [Display(Name = "Кабинет")]
        [Required(ErrorMessage = "Кабинет для бронирования обязателен")]
        public Room Room { get; init; }

        [Display(Name = "Дата создания")]
        public DateTime CreationDate { get; init; }

        [Range(0, 4,
            ErrorMessage = "Статус бронирования кабинета должен быть от 0 до 3 (0-не подтвержден, 1-отменен, 2-забронирован, 3-завершен)")]
        [Display(Name = "Статус бронирования")]
        public BookingStatus Status { get; init; }

        [Display(Name = "Дата и время начала бронирования")]
        public DateTime? TimeBegin { get; init; }
        [Display(Name = "Дата и время окончания бронирования")]
        public DateTime? TimeEnd { get; init; }

    }
}
