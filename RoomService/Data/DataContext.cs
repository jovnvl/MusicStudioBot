using Microsoft.EntityFrameworkCore;
using RoomService.Models.Entities;

namespace RoomService.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Room> Rooms {  get; set; }
        public DbSet<CategoryRoom> CategoryRooms{ get; set; }
        public DbSet<OutboundMessages> OutboundMessages { get; set; }
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }


    }
}
