using System.Threading;
using System.Threading.Tasks;
using OpenAI.Embeddings;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Infrastructure.Configuration;

namespace TemperaturePredictionService.Infrastructure.AI
{
    public sealed class OpenAiClientAdapter : IEmbeddingClientAdapter
    {
        private readonly EmbeddingClient _client;

        public OpenAiClientAdapter(OpenAiOptions opts)
        {
            _client = new EmbeddingClient(opts.Model, opts.ApiKey);
        }

        public async Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
        {
         
            var embedding = (await _client.GenerateEmbeddingAsync(text, cancellationToken: ct)).Value;

           
            return embedding.ToFloats().ToArray();
        }
    }
}
