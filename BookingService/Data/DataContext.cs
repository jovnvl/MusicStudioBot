using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BookingService.Models.Entities;

namespace BookingService.Data
{
    public class DataContext : DbContext
    {
        public DbSet<CategoryRoom> CategoryRooms { get; set; }
        public DbSet<Room> Rooms {  get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        public DataContext(DbContextOptions<DataContext> options) : base(options) { }


    }
}
