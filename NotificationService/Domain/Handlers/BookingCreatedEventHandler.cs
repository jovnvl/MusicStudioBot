using NotificationService.Models.Entities;
using NotificationService.Data;
using NotificationService.Domain.Events;

namespace NotificationService.Domain.Handlers
{ 
    public class BookingCreatedEventHandler : IEventHandler<BookingCreatedEvent>
    {
        private readonly DataContext _db;

        public BookingCreatedEventHandler(DataContext db) => _db = db;

        public async Task HandleAsync(BookingCreatedEvent notification, CancellationToken ct)
        {
            // Пример: напоминание через 3 минуты
            var remindAt = notification.CreatedAt.AddMinutes(3);

            var job = new ReminderJob
            {
                BookingId = notification.BookingId,
                RemindAt = remindAt,
                Attempts = 0,
                Status = ReminderJobStatus.Pending
            };

            _db.ReminderJobs.Add(job);
            await _db.SaveChangesAsync(ct);
        }
    }
}
