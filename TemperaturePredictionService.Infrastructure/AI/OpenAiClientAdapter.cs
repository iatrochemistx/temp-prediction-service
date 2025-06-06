using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Infrastructure.Configuration;

namespace TemperaturePredictionService.Infrastructure.AI
{
    public sealed class OpenAiClientAdapter : IEmbeddingClientAdapter
    {
            private readonly EmbeddingClient _client;

        public OpenAiClientAdapter(IOptions<OpenAiOptions> opts)
        {
            var openAiOpts = opts.Value;
            _client = new EmbeddingClient(openAiOpts.Model, openAiOpts.ApiKey);
        }

        public async Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
        {

            var embedding = (await _client.GenerateEmbeddingAsync(text, cancellationToken: ct)).Value;


            return embedding.ToFloats().ToArray();
        }
    }
}
