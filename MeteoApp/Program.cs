using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using MeteoApp.Data;
using MeteoApp.Services;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace MeteoApp
{
    public class Program
    {
        private static System.Timers.Timer? _hourlyTimer; // Explicitní specifikace System.Timers.Timer
        private static IServiceProvider? _serviceProvider;
        private static string _currentWeatherStationUrl = ""; // Pro ukládání aktuální URL

        public static async Task Main(string[] args)
        {
            // Vytvoření hostitele pro konfiguraci a Dependency Injection
            var host = CreateHostBuilder(args).Build();
            _serviceProvider = host.Services; // Uložení ServiceProvider pro přístup ke službám

            // Aplikace migrací databáze při startu
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();
            }

            // Načtení výchozí URL z konfigurace
            var config = _serviceProvider.GetRequiredService<IConfiguration>();
            _currentWeatherStationUrl = config["WeatherStationUrl"] ?? "https://pastebin.com/raw/PMQueqDV";

            Console.WriteLine("Vítejte v MeteoApp!");
            await RunApplicationMenu();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Načtení appsettings.json
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Přidání kontextu databáze
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection")));

                    // Přidání služeb do DI kontejneru
                    services.AddTransient<WeatherService>();
                    services.AddTransient<DatabaseService>();
                    services.AddHttpClient(); // Přidání HttpClient pro injekci do WeatherService
                });

        private static async Task RunApplicationMenu()
        {
            bool exitApp = false;
            while (!exitApp)
            {
                Console.WriteLine("\n--- Menu MeteoApp ---");
                Console.WriteLine("1. Stáhnout data (výchozí URL)");
                Console.WriteLine("2. Stáhnout data (vlastní URL)");
                Console.WriteLine("3. Zobrazit poslední stažená data");
                Console.WriteLine("4. Spustit hodinový timer");
                Console.WriteLine("5. Zastavit hodinový timer");
                Console.WriteLine("6. Ukončit aplikaci");
                Console.Write("Zadejte volbu: ");

                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await DownloadAndSaveData(_currentWeatherStationUrl);
                        break;
                    case "2":
                        Console.Write("Zadejte vlastní URL meteostanice: ");
                        string? customUrl = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(customUrl))
                        {
                            await DownloadAndSaveData(customUrl);
                        }
                        else
                        {
                            Console.WriteLine("URL nesmí být prázdná.");
                        }
                        break;
                    case "3":
                        await DisplayLastReading();
                        break;
                    case "4":
                        StartHourlyTimer();
                        break;
                    case "5":
                        StopHourlyTimer();
                        break;
                    case "6":
                        exitApp = true;
                        StopHourlyTimer(); // Zastavíme timer před ukončením
                        Console.WriteLine("Aplikace se ukončuje.");
                        break;
                    default:
                        Console.WriteLine("Neplatná volba. Zkuste to znovu.");
                        break;
                }
            }
        }

        private static async Task DownloadAndSaveData(string url)
        {
            if (_serviceProvider == null)
            {
                Console.WriteLine("Chyba: ServiceProvider není inicializován.");
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var weatherService = scope.ServiceProvider.GetRequiredService<WeatherService>();
                var databaseService = scope.ServiceProvider.GetRequiredService<DatabaseService>();

                Console.WriteLine($"Stahuji data z URL: {url}...");
                var reading = await weatherService.GetWeatherReadingAsync(url); // Předáváme URL
                await databaseService.SaveWeatherReadingAsync(reading);

                Console.WriteLine($"Záznam uložen. Čas stažení: {reading.DownloadTime}, Meteostanice dostupná: {reading.IsAvailable}");
                if (!reading.IsAvailable)
                {
                    Console.WriteLine("Meteostanice nebyla dostupná nebo došlo k chybě při stahování dat.");
                }
                else
                {
                    Console.WriteLine($"Stažená data (JSON): {reading.Data}");
                }
            }
        }

        private static async Task DisplayLastReading()
        {
            if (_serviceProvider == null)
            {
                Console.WriteLine("Chyba: ServiceProvider není inicializován.");
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var databaseService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
                var lastReading = await databaseService.GetLastWeatherReadingAsync();

                if (lastReading != null)
                {
                    Console.WriteLine("\n--- Poslední stažená data ---");
                    Console.WriteLine($"ID: {lastReading.Id}");
                    Console.WriteLine($"Čas stažení: {lastReading.DownloadTime}");
                    Console.WriteLine($"Meteostanice dostupná: {lastReading.IsAvailable}");
                    if (lastReading.IsAvailable && !string.IsNullOrEmpty(lastReading.Data))
                    {
                        Console.WriteLine($"Data (JSON): {lastReading.Data}");
                    }
                    else
                    {
                        Console.WriteLine("Data nejsou dostupná (meteostanice byla offline nebo data chybí).");
                    }
                }
                else
                {
                    Console.WriteLine("V databázi nejsou žádné záznamy.");
                }
            }
        }

        private static void StartHourlyTimer()
        {
            if (_hourlyTimer == null)
            {
                _hourlyTimer = new System.Timers.Timer(3600000); // Explicitní specifikace System.Timers.Timer
                _hourlyTimer.Elapsed += async (sender, e) => await Timer_Elapsed(sender, e);
                _hourlyTimer.AutoReset = true; // Opakovat se automaticky
                _hourlyTimer.Start();
                Console.WriteLine($"Hodinový timer spuštěn. Data se budou stahovat každou hodinu.");
            }
            else if (!_hourlyTimer.Enabled)
            {
                _hourlyTimer.Start();
                Console.WriteLine("Hodinový timer byl znovu spuštěn.");
            }
            else
            {
                Console.WriteLine("Hodinový timer již běží.");
            }
        }

        private static void StopHourlyTimer()
        {
            if (_hourlyTimer != null && _hourlyTimer.Enabled)
            {
                _hourlyTimer.Stop();
                Console.WriteLine("Hodinový timer zastaven.");
            }
            else
            {
                Console.WriteLine("Hodinový timer neběží.");
            }
        }

        private static async Task Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Console.WriteLine($"\n--- Automatické stahování dat (Timer) v {DateTime.Now} ---");
            await DownloadAndSaveData(_currentWeatherStationUrl);
            Console.WriteLine("--- Automatické stahování dokončeno ---");
        }
    }
}