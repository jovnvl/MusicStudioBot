using BookingService.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace BookingService.DTO

{
    public class BookingDto
    {
        /*[Display(Name = "Id бронирования кабинета")]
        [Required(ErrorMessage = "ID бронирования кабинета обязателен")]
        public Guid Id { get; init; } = Guid.NewGuid();
        */
        [StringLength(500,
        ErrorMessage = "Описание не должно превышать 500 символов")]
        [Display(Name = "Описание")]
        public string Description { get; init; } = string.Empty;

        [Display(Name = "Пользователь")]
        [Required(ErrorMessage = "Кто бронирует обязателен")]
        public Guid UserId { get; init; } = Guid.Parse("284616f4-e14d-4f05-af9b-baef54d9a6eb");
        /*public User User { get; init; } = new User()
        {
            Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            TelegramId = 10L,
            Username = "Krause",
            FirstName = "Mt.Krause",
            LastName = "Metr",
            Role = UserRole.Student,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
        };
        */
        [Display(Name = "Кабинет")]
        [Required(ErrorMessage = "Кабинет для бронирования обязателен")]
        public int RoomId { get; init; } = 1;
        /*public Room Room { get; init; } = new Room()
        {
            Name = "Kab.134",
            Description = "Fortepiano",
            CreationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = RoomStatus.Available,
            CategoryRoomId = 1,
            CategoryRoom = new CategoryRoom()
            {
                Name = "Music",
                Description ="For piano",
            }
        };
        */
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
