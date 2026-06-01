using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BookingService.Models.Entities;

namespace BookingService.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Booking> Bookings { get; set; }
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>(b =>
            {
                //1. json не десериализует!
                b.OwnsOne(x => x.Period, p =>
                {
                    p.Property(x => x.TimeBegin).HasColumnName("TimeBegin");
                    p.Property(x => x.TimeEnd).HasColumnName("TimeEnd");
                });

                b.Navigation(x => x.Period)
                    .IsRequired();
            });
        }
    }
}
