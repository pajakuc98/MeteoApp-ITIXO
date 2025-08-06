using Microsoft.EntityFrameworkCore;
using MeteoApp.Models;

namespace MeteoApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WeatherReading> WeatherReadings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Zde můžete přidat další konfiguraci modelu, pokud je potřeba
            base.OnModelCreating(modelBuilder);
        }
    }
}