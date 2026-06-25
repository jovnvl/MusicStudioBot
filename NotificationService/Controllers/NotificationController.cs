using NotificationService.Models.Entities;
using NotificationService.Services;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotiferService _notiferService;
        public NotificationController(INotiferService notiferService)
        {
            _notiferService = notiferService;
        }

        [HttpGet("ReminderJobs")]
        public async Task<IActionResult> GetReminderJobs(
        [FromQuery] long Id,
        [FromQuery] Guid BookingId,
        [FromQuery] DateTime RemindAt,
        [FromQuery] DateTime? LockedAt,
        [FromQuery] int Attempts,
        [FromQuery] string? LastError,
        [FromQuery] ReminderJobStatus Status,
        CancellationToken ct = default)
        {
            var reminderJobs = new ReminderJob()
            {
                Id = Id,
                BookingId = BookingId,
                RemindAt = RemindAt,
                LockedAt = LockedAt,
                Attempts = Attempts,
                LastError = LastError,
                Status = Status
            };

            var jobs = await _notiferService.GetReminderJobsAsync(reminderJobs, ct);
            return Ok(jobs);
        }
    }
}
