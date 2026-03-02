using System.ComponentModel.DataAnnotations;

namespace BookingService.DTO

{
    public class UpdateRoomDto
    {
        [Required(ErrorMessage = "ID комнаты обязателен")]
        [Range(1, int.MaxValue,
            ErrorMessage = "ID комнаты должен быть положительным числом")]
        [Display(Name = "ID комнаты комнаты")]
        public int Id { get; init; }

        [Required(ErrorMessage = "Статус комнаты обязателен")]
        [Range(0, 3,
            ErrorMessage = "Статус должен быть от 0 до 3 (0-свободна, 1-занята, 2-бронирование, 3-недоступна)")]
        [Display(Name = "Статус комнаты")]
        public RoomStatus Status { get; init; }
    }
}
