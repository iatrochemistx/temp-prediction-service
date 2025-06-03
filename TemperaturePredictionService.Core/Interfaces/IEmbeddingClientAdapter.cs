namespace TemperaturePredictionService.Core.Interfaces
{
    public interface IEmbeddingClientAdapter
    {
        Task<float[]> GenerateAsync(string text, CancellationToken ct = default);
    }
}
