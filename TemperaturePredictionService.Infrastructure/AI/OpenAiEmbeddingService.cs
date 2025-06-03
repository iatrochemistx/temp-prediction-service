using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Infrastructure.Configuration;

namespace TemperaturePredictionService.Infrastructure.AI
{
    public sealed class OpenAiEmbeddingService : IEmbeddingService
    {
        private readonly IEmbeddingClientAdapter _embeddingClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<OpenAiEmbeddingService> _log;
        private readonly OpenAiOptions _opts;

        public OpenAiEmbeddingService(
            IEmbeddingClientAdapter embeddingClient,  
            IOptions<OpenAiOptions> opts,
            IMemoryCache cache,
            ILogger<OpenAiEmbeddingService> log)
        {
            _embeddingClient = embeddingClient;
            _opts = opts.Value;
            _cache = cache;
            _log = log;
        }

        public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(text, out float[] cached))
                return cached;

            var vector = await _embeddingClient.GenerateAsync(text, ct);

            _cache.Set(text, vector, TimeSpan.FromDays(_opts.CacheDays));
            return vector;
        }
    }
}
