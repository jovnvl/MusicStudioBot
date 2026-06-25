using NotificationService.Models.Entities;
using NotificationService.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Services
{
    public class BookingReminderDispatcher : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<BookingReminderDispatcher> _log;

        // сколько времени job может быть “залочен” чтобы его можно было переподнять
        private readonly TimeSpan _lockTtl = TimeSpan.FromMinutes(2);

        public BookingReminderDispatcher(IServiceProvider sp, ILogger<BookingReminderDispatcher> log)
        {
            _sp = sp;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // частота опроса
            var delay = TimeSpan.FromSeconds(10);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_log.IsEnabled(LogLevel.Information))
                    {
                        _log.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    }
                    await Tick(stoppingToken);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Reminder dispatcher tick failed");
                }

                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task Tick(CancellationToken ct)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            // и сервис уведомлений:
            var notifier = scope.ServiceProvider.GetRequiredService<INotiferService>();

            var now = DateTime.UtcNow;

            // Забираем небольшой батч
            var jobs = new List<ReminderJob>();

            using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);

            // claim: берём Pending due и/или просроченные locked (если воркер упал)
            jobs = await db.ReminderJobs
                .Where(x =>
                    x.Status == ReminderJobStatus.Pending &&
                    x.RemindAt <= now)
                .OrderBy(x => x.RemindAt)
                .Take(10)
                .ToListAsync(ct);

            foreach (var j in jobs)
            {
                j.Status = ReminderJobStatus.Processing;
                j.LockedAt = now;
                j.Attempts += 1;
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Исполняем снаружи транзакции
            foreach (var job in jobs)
            {
                try
                {
                    // тут нужно достать booking по BookingId
                    // или передать контент уведомления в job при создании
                    await notifier.NotifyBookingReminderAsync(job.BookingId, ct);

                    using var scope2 = _sp.CreateScope();
                    var db2 = scope2.ServiceProvider.GetRequiredService<DataContext>();

                    var fresh = await db2.ReminderJobs.FirstOrDefaultAsync(x => x.Id == job.Id, ct);
                    fresh?.Status = ReminderJobStatus.Sent;
                    fresh?.LastError = null;
                    fresh?.LockedAt = null;

                    await db2.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Send reminder failed, jobId={JobId}", job.Id);

                    using var scope3 = _sp.CreateScope();
                    var db3 = scope3.ServiceProvider.GetRequiredService<DataContext>();

                    var fresh = await db3.ReminderJobs.FirstOrDefaultAsync(x => x.Id == job.Id, ct);
                    // простая стратегия ретраев
                    if (fresh?.Attempts >= 5)
                    {
                        fresh.Status = ReminderJobStatus.Failed;
                    }
                    else
                    {
                        fresh?.Status = ReminderJobStatus.Pending;
                        // можно backoff
                        fresh?.RemindAt = DateTime.UtcNow.AddMinutes(1);
                    }

                    fresh?.LastError = ex.Message;
                    fresh?.LockedAt = null;

                    await db3.SaveChangesAsync(ct);
                }
            }
        }
    }
}
