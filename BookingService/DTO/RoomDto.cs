using System.ComponentModel.DataAnnotations;

namespace BookingService.DTO

{
    public class RoomDto
    {
        [Required(ErrorMessage = "Название комнаты обязательно")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "Название должно быть от 2 до 50 символов")]
        [Display(Name = "Название комнаты")]
        public string Name { get; init; } = string.Empty;

        [StringLength(500,
            ErrorMessage = "Описание не должно превышать 500 символов")]
        [Display(Name = "Описание")]
        public string Description { get; init; } = string.Empty;
        /*
        [DataType(DataType.Upload)]
        [MaxLength(5 * 1024 * 1024, // 5MB
            ErrorMessage = "Фото не должно превышать 5 МБ")]
        [Display(Name = "Фото комнаты")]
        public byte[]? Photo { get; init; }
        */
        [Required(ErrorMessage = "ID категории комнаты обязателен")]
        [Range(1, int.MaxValue,
            ErrorMessage = "ID категории должен быть положительным числом")]
        [Display(Name = "ID категории комнаты")]
        public int CategoryRoomId { get; init; } = 1;

        [Required(ErrorMessage = "Статус комнаты обязателен")]
        [Range(0, 3,
            ErrorMessage = "Статус должен быть от 0 до 3 (0-свободна, 1-занята, 2-бронирование, 3-недоступна)")]
        [Display(Name = "Статус комнаты")]
        public RoomStatus Status { get; init; }
    }
}
