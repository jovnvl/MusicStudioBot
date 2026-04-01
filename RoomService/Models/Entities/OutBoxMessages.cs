using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace RoomService.Models.Entities
{
    [Table("outbox_messages")]
    public class OutBoxMessages
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("service")]
        public string Service { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("level")]
        public string Level { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("event_type")]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

    }
    
    
}
