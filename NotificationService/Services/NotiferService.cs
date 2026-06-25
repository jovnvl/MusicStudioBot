using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models.Entities;
using NotificationService.Models.Entities.DTO;
using System.Net.NetworkInformation;

namespace NotificationService.Services
{
    public sealed class NotiferService : INotiferService
    {
        private readonly ILogger<NotiferService> _logger;


        private readonly DataContext _db;

        public NotiferService(ILogger<NotiferService> logger, DataContext db)
        {
            _logger = logger;
            _db = db;
        }

        public Task NotifyBookingReminderAsync(Guid bookingId, CancellationToken ct = default)
        {
            _logger.LogInformation("NotifyBookingReminder: BookingId={BookingId}", bookingId);
            return Task.CompletedTask;
        }

        public async Task UpsertNotiferAsync(NotificationDto notificationDto, CancellationToken ct)
        {
            await UpsertReminderJobAsync(notificationDto, ct);
        }

        public async Task<IReadOnlyList<ReminderJob>> GetNotifersAsync(ReminderJob job, CancellationToken ct)
        {
            var reminderJobs = await GetReminderJobsAsync(job, ct);
            return reminderJobs;
        }


        public async Task UpsertReminderJobAsync(NotificationDto dto, CancellationToken ct)
        {
            bool isDelete =
                   dto.EventType.Equals("delete-booking", StringComparison.OrdinalIgnoreCase);

            var bookingId = dto.BookingDto.Id;

            var existing = await _db.ReminderJobs
                .FirstOrDefaultAsync(x => x.BookingId == bookingId, ct);

            if (isDelete)
            {
                if (existing == null) return;

                _db.ReminderJobs.Remove(existing);
                await _db.SaveChangesAsync(ct);
                return;
            }

            if (existing == null)
            {
                var job = new ReminderJob
                {
                    BookingId = bookingId,
                    RemindAt = dto.BookingDto.TimeBegin,

                    Attempts = 0,
                    Status = ReminderJobStatus.Pending,

                    Timestamp = dto.Timestamp,
                    EventType = dto.EventType,

                    LockedAt = null,
                    LastError = string.Empty
                };

                await _db.ReminderJobs.AddAsync(job, ct);
                await _db.SaveChangesAsync(ct);
                return;
            }

            // UPDATE
            bool shouldRequeue =
                    dto.EventType.Equals("create-booking", StringComparison.OrdinalIgnoreCase) ||
                    dto.EventType.Equals("update-booking", StringComparison.OrdinalIgnoreCase);

            if (shouldRequeue && existing.Status is ReminderJobStatus.Pending or ReminderJobStatus.Failed)
            {
                existing.Status = ReminderJobStatus.Pending;
                // attempts не сбрасываем
            }

            // при "перебронировании" наверное поттребуется сброс attempts :
            if (dto.EventType.Equals("replace-booking", StringComparison.OrdinalIgnoreCase)) 
                existing.Attempts = 0;

            existing.LockedAt = null; // делаем перезапуск для снятия lock
            existing.LastError = string.Empty;

            existing.Timestamp = dto.Timestamp;
            existing.EventType = dto.EventType;

            existing.RemindAt = dto.BookingDto.TimeBegin;
            existing.Status = ReminderJobStatus.Pending;
            existing.LockedAt = null;
            existing.LastError = string.Empty;

            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<ReminderJob>> GetReminderJobsAsync(ReminderJob reminderJob, CancellationToken ct)
        {
            /*
            var query = _db.ReminderJobs.AsQueryable<ReminderJob>();
            query = query.Where(x => x.RemindAt >= reminderJob.RemindAt);

            if (reminderJob.LockedAt.HasValue)
                query = query.Where(x => x.LockedAt >= reminderJob.LockedAt.Value);

            if (!string.IsNullOrEmpty(reminderJob.LastError))
                query = query.Where(x => x.LastError == reminderJob.LastError);

            if (reminderJob.BookingId != Guid.Empty)
                query = query.Where(x => x.BookingId == reminderJob.BookingId);

            if (!string.IsNullOrEmpty(reminderJob.Status.ToString()))
                query = query.Where(x => x.Status == reminderJob.Status);

            var items = await query.OrderByDescending(l => l.RemindAt)
                                   .ToListAsync(ct);
            
            return items;
            */
            return await _db.ReminderJobs.AsNoTracking().OrderByDescending(l => l.RemindAt).ToListAsync(ct);
        }

    }
}
