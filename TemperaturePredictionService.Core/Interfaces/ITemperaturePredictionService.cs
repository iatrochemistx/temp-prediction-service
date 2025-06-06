public interface ITemperaturePredictionService
{
    Task<float> PredictTemperatureAsync(DateTime date, string city, CancellationToken ct = default);
}
