
using System.Collections.Generic;
using TemperaturePredictionService.Core.Models;


namespace TemperaturePredictionService.Core.Interfaces 
{
   public interface ITemperatureDataLoader
{
    IAsyncEnumerable<TemperatureRecord> LoadRecords(
        string? filePath = null,
        CancellationToken ct = default);
}

}
