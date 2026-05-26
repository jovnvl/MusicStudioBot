using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BookingService.Models.Entities;

namespace BookingService.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Booking> Bookings { get; set; }
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }


    }
}
