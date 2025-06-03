using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;      // v2.x namespace
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Infrastructure.Configuration;

namespace TemperaturePredictionService.Infrastructure.AI;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly OpenAIClient        _client;
    private readonly IMemoryCache        _cache;
    private readonly ILogger<OpenAiEmbeddingService> _log;
    private readonly OpenAiOptions       _opts;

    public OpenAiEmbeddingService(
        OpenAIClient client,
        IMemoryCache cache,
        IOptions<OpenAiOptions> opts,
        ILogger<OpenAiEmbeddingService> log)
    {
        _client = client;
        _cache  = cache;
        _opts   = opts.Value;
        _log    = log;
    }

    public async Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken ct = default)
    {
        if (_cache.TryGetValue(text, out float[] cached))
            return cached;

        // v2.x style call
        EmbeddingResponse resp = await _client.Embeddings.CreateAsync(
            new EmbeddingRequest
            {
                Model = _opts.Model,
                Input = text
            },
            ct);

        float[] vector = resp.Data[0].Embedding;

        _cache.Set(text, vector, TimeSpan.FromDays(_opts.CacheDays));
        return vector;
    }
}
