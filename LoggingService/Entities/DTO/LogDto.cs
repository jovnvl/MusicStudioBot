using System.ComponentModel.DataAnnotations;

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
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;
    }
}
