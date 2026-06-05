using LoggingService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoggingService.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Log> Logs { get; set; }
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }


    }
}
