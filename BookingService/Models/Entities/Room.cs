using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BookingService.Models.Entities
{
    [Table("Room")]
    [Index(nameof(CategoryRoomId), Name = "IDX_RoomCategoryRoomId")]
    [Index(nameof(Name), IsUnique = true, Name = "IDX_RoomName")]
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Column("Description")]
        public string Description { get; set; } = string.Empty;

        [Column("Photo", TypeName = "bytea")]
        public byte[]? Photo { get; set; }

        [Required]
        [Column("CreationDate", TypeName = "date")]
        public DateOnly CreationDate { get; set; }

        [Required]
        [ForeignKey(nameof(CategoryRoom))]
        [Column("CategoryRoomId")]
        public int CategoryRoomId { get; set; }

        [Required]
        [JsonIgnore]
        public CategoryRoom CategoryRoom { get; set; } = null!;

        [Required]
        [Column("Status")]
        public RoomStatus Status { get; set; }
    }
}
