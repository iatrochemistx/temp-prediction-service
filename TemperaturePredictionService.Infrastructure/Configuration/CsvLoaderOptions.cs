namespace TemperaturePredictionService.Infrastructure.Configuration
{
    public record CsvLoaderOptions
    {
        public string DefaultCsvPath { get; init; } = "data/temperatures.csv";
    }
}
