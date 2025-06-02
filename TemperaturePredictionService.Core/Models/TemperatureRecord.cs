using System;

namespace TemperaturePredictionService.Core.Models
{
    public class TemperatureRecord
    {
        public DateTime Date { get; set; }
        public string City { get; set; } = default!;
        public float Temperature { get; set; }
    }
}
