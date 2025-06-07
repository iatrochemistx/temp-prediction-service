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
            //_log.LogInformation("Connecting to OpenAI API for embedding: {Text}", text);
            var vector = await _embeddingClient.GenerateAsync(text, ct);
            //_log.LogInformation("Received embedding from OpenAI API for: {Text}", text);

            // Log embedding values ONCE per city per run
            if (!_cache.TryGetValue($"LOG:{text}", out _))
            {
                _log.LogInformation("Embed - {City}: {First}", text, string.Join(", ", vector.Take(5)));
                _cache.Set($"LOG:{text}", true); // log once per city per run
            }

            _cache.Set(text, vector, TimeSpan.FromDays(_opts.CacheDays));
            return vector;
        }

    }
}
