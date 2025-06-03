namespace TemperaturePredictionService.Infrastructure.Configuration
{
    public sealed class OpenAiOptions
    {
        public string ApiKey { get; init; } = string.Empty;
        public string Model  { get; init; } = "text-embedding-3-small";
        public int    CacheDays { get; init; } = 30;
    }
}
