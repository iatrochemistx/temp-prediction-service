
using System.Collections.Generic;
using TemperaturePredictionService.Core.Models;


namespace TemperaturePredictionService.Core.Interfaces 
{
    public interface ITemperatureDataLoader
{
    IEnumerable<TemperatureRecord> LoadRecords(string filePath);
}
}
