using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace BookingService.Models.Entities
{
    [Table("rooms")]
    [Index(nameof(CategoryRoomId), Name = "IDX_rooms_category_room_id")]
    public class Room
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("photo", TypeName = "bytea")]
        public byte[]? Photo { get; set; }

        [Required]
        [Column("creation_date", TypeName = "date")]
        public DateOnly CreationDate { get; set; }

        [Required]
        [ForeignKey(nameof(CategoryRoom))]
        [Column("category_room_id")]
        public int CategoryRoomId { get; set; }

        [Required]
        [JsonIgnore]
        public CategoryRoom CategoryRoom { get; set; } = null!;

        [Required]
        [Column("status")]
        public RoomStatus Status { get; set; }
    }
}
