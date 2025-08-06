using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeteoApp.Models
{
    public class WeatherReading
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime DownloadTime { get; set; }

        public bool IsAvailable { get; set; }

        // Uložení JSONu jako string
        public string? Data { get; set; }
    }
}