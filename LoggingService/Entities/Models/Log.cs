using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace LoggingService.Models.Entities
{
    [Table("Logs")]
    [Index(nameof(Timestamp), Name = "IDX_logs_timestamp")]
    [Index(nameof(Service), Name = "IDX_logs_service")]
    [Index(nameof(Level), Name = "IDX_logs_level")]
    [Index(nameof(EventType), Name = "IDX_logs_event_type")]
    public class Log
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
        [MaxLength(50)]
        [Column("event_type")]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Column("message")]
        public string Message { get; set; } = string.Empty;
    }
}
