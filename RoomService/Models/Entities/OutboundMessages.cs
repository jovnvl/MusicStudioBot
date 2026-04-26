using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace RoomService.Models.Entities
{
    [Table("outbound_messages")]
    public class OutboundMessages
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("service")]
        public string Service { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("level")]
        public string Level { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("event_type")]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Column("status")]
        public MessageStatus Status { get; set; }

        [Column("processed_at")]
        public DateTime ProcessedAt { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("queue_name")]
        public string QueueName { get; set; } = string.Empty;

    }
    
    
}
