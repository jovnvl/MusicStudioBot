using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoomService.Models.Entities;

namespace RoomService.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Room> Rooms {  get; set; }
        public DbSet<CategoryRoom> CategoryRooms{ get; set; }
        public DataContext()
        {
            //Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {

            optionsBuilder.UseNpgsql("Host=localhost;Username=postgres;Password=admin;Database=MusicStudio");
            base.OnConfiguring(optionsBuilder);
        }
    }
}
