using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace LoggingService.Models.Entities.DTO
{

    public class LogFilterDto
    {
        public DateTime? from { get; set; }

        public DateTime? to { get; set; }

        [MaxLength(100)]
        public string? service { get; set; }

        [MaxLength(30)]
        public string? level { get; set; }

        [MaxLength(20)]
        public string? eventType { get; set; }

        [Required]
        public int page { get; set; } = 1;

        [Required]
        public int pageSize { get; set; } = 100;
    }
}
