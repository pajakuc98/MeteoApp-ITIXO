using System.Threading.Tasks;
using MeteoApp.Data;
using MeteoApp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore; 

namespace MeteoApp.Services
{
    public class DatabaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ApplicationDbContext context, ILogger<DatabaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SaveWeatherReadingAsync(WeatherReading reading)
        {
            try
            {
                _context.WeatherReadings.Add(reading);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Záznam o počasí úspěšně uložen do databáze. ID: {reading.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Chyba při ukládání záznamu do databáze: {ex.Message}");
                // Zde můžete zvážit další logiku pro zpracování chyby, např. opakování
            }
        }

        
        public async Task<WeatherReading?> GetLastWeatherReadingAsync()
        {
            try
            {
                // Získáme poslední záznam podle času stažení
                return await _context.WeatherReadings
                                     .OrderByDescending(r => r.DownloadTime)
                                     .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Chyba při načítání posledního záznamu z databáze: {ex.Message}");
                return null;
            }
        }
    }
}