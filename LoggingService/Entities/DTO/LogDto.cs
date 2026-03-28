using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace LoggingService.Models.Entities.DTO
{

    public class LogDto
    {
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(100)]
        public string Service { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Level { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;
    }
}
