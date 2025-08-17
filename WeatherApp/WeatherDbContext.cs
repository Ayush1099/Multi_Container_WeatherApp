using Microsoft.EntityFrameworkCore;

namespace WeatherApp
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

        public DbSet<Weather> WeatherEntries { get; set; }
    }
}
