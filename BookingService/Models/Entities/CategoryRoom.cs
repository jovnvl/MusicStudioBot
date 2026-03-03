using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BookingService.Models.Entities
{
    [Table("CategoryRoom")]
    public class CategoryRoom
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

        [JsonIgnore]
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
