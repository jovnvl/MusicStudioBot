using NotificationService.Models.Entities;
using NotificationService.Models.Entities.DTO;

namespace NotificationService.Services
{
    public interface INotiferService
    {
        Task NotifyBookingReminderAsync(Guid bookingId, CancellationToken ct);
        public Task UpsertNotiferAsync(NotificationDto notificationDto, CancellationToken ct);
        public Task<IReadOnlyList<ReminderJob>> GetReminderJobsAsync(ReminderJob reminderJob, CancellationToken ct);
    }
}
