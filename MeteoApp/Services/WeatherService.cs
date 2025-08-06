using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.Json;
using MeteoApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization; 
using System.Linq; 

namespace MeteoApp.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _defaultWeatherStationUrl;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _defaultWeatherStationUrl = configuration["WeatherStationUrl"] ?? throw new ArgumentNullException("WeatherStationUrl není nakonfigurována.");
            _logger = logger;
        }

        // Metoda nyní přijímá volitelnou URL
        public async Task<WeatherReading> GetWeatherReadingAsync(string? url = null)
        {
            string finalUrl = url ?? _defaultWeatherStationUrl; // Použijeme zadanou URL, nebo výchozí

            try
            {
                _logger.LogInformation($"Pokouším se stáhnout data z URL: {finalUrl}");
                var xmlString = await _httpClient.GetStringAsync(finalUrl);
                _logger.LogInformation("Data úspěšně stažena.");

                var xDoc = XDocument.Parse(xmlString);

                // Parsování XML dat pomocí pomocné metody
                var weatherData = new WeatherData
                {
                    Temperature = GetSensorValue<double>(xDoc, "temperature"),
                    Humidity = GetSensorValue<double>(xDoc, "humidity"),
                    Pressure = GetSensorValue<double>(xDoc, "pressure"),
                    WindSpeed = GetSensorValue<double>(xDoc, "wind_speed"),
                    WindDirection = GetSensorValue<string>(xDoc, "wind_direction")
                };

                // Převod objektu WeatherData na JSON string
                var json = JsonSerializer.Serialize(weatherData, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("XML data úspěšně transformována na JSON.");

                return new WeatherReading
                {
                    DownloadTime = DateTime.Now,
                    IsAvailable = true,
                    Data = json
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"Chyba při stahování dat z meteostanice (HTTP): {ex.Message}");
                return CreateUnavailableReading();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Obecná chyba při zpracování dat z meteostanice: {ex.Message}");
                return CreateUnavailableReading();
            }
        }

        /// <summary>
        /// Vyhledá hodnotu senzoru v XML dokumentu podle jeho typu.
        /// </summary>
        /// <typeparam name="T">Typ hodnoty, která má být vrácena (např. double, string).</typeparam>
        /// <param name="xDoc">XML dokument.</param>
        /// <param name="sensorType">Typ senzoru (např. "temperature", "wind_speed").</param>
        /// <returns>Hodnota senzoru převedená na typ T, nebo defaultní hodnota T, pokud senzor není nalezen nebo dojde k chybě parsování.</returns>
        private T GetSensorValue<T>(XDocument xDoc, string sensorType)
        {
            // Najdeme element <sensor>, který má dítě <type> s odpovídající hodnotou
            var sensorElement = xDoc.Descendants("sensor")
                                    .FirstOrDefault(s => s.Element("type")?.Value == sensorType);

            if (sensorElement == null)
            {
                _logger.LogWarning($"Senzor typu '{sensorType}' nebyl nalezen v XML. Vrácena výchozí hodnota.");
                return default!;
            }

            // Získáme hodnotu z elementu <value> uvnitř nalezeného senzoru
            var valueElement = sensorElement.Element("value");
            if (valueElement == null)
            {
                _logger.LogWarning($"Element 'value' nebyl nalezen pro senzor typu '{sensorType}'. Vrácena výchozí hodnota.");
                return default!;
            }

            try
            {
                if (typeof(T) == typeof(double))
                {
                    // Použijeme invariantní kulturu pro parsování double, aby se předešlo problémům s desetinnou čárkou/tečkou
                    return (T)Convert.ChangeType(valueElement.Value, typeof(T), CultureInfo.InvariantCulture);
                }
                return (T)Convert.ChangeType(valueElement.Value, typeof(T));
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, $"Chyba formátu při parsování hodnoty '{valueElement.Value}' pro senzor typu '{sensorType}'.");
                return default!;
            }
            catch (InvalidCastException ex)
            {
                _logger.LogError(ex, $"Chyba převodu typu při parsování hodnoty '{valueElement.Value}' pro senzor typu '{sensorType}'.");
                return default!;
            }
        }

        private WeatherReading CreateUnavailableReading()
        {
            return new WeatherReading
            {
                DownloadTime = DateTime.Now,
                IsAvailable = false,
                Data = null // Žádná data, protože meteostanice nebyla dostupná
            };
        }
    }
}