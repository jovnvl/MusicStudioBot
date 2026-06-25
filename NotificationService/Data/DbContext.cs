using NotificationService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Data
{
    public class DataContext : DbContext
    {
        public DbSet<ReminderJob> ReminderJobs { get; set; }

        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReminderJob>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.RemindAt);
                e.HasIndex(x => new { x.Status, x.RemindAt });
            });
        }
    }
}
