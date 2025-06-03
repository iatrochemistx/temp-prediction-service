using System.Threading;
using System.Threading.Tasks;

namespace TemperaturePredictionService.Core.Interfaces
{
    /// <summary>
    /// Provides a numeric embedding for input text.
    /// </summary>
    public interface IEmbeddingService
    {
        Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
    }
}
