using System;

namespace TemperaturePredictionService.Core.Models
{
    public class TemperatureRecord
    {
        public DateTime Date { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Region { get; set; }
        public float Temperature { get; set; }
    }

}
