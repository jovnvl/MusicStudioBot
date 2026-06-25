using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationService.Models.Entities
{
 
    public class ReminderJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Guid BookingId { get; set; }
        public DateTime RemindAt { get; set; }
        public int Attempts { get; set; }
        public ReminderJobStatus Status { get; set; }
        public DateTime? LockedAt { get; set; }
        public string? LastError { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
    }

    public enum ReminderJobStatus
    {
        Pending = 0,
        Processing = 1,
        Sent = 2,
        Failed = 3
    }
}
