using System.ComponentModel.DataAnnotations;
using System.Formats.Asn1;

namespace RoomService.DTO
{
    public class CreateCategoryRoomDto
    {
        [Required(ErrorMessage = "Название категории обязательно")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "Название категории должно быть от 2 до 50 символов")]
        [Display(Name = "Название категории")]
        public string Name { get; init; }

        [Required(ErrorMessage = "Описание категории обязательно")]
        [StringLength(500,
           ErrorMessage = "Описание категории не должно превышать 500 символов")]
        [Display(Name = "Описание категории")]
        public  string Description { get; init; }
    }
}
