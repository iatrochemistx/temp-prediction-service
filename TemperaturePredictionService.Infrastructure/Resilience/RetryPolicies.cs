// File: TemperaturePredictionService.Infrastructure/Resilience/RetryPolicies.cs

using System;
using System.Net.Http;
using System.ClientModel;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace TemperaturePredictionService.Infrastructure.Resilience
{
    public static class RetryPolicies
    {
        // Retries on ClientResultException (HTTP 429/5xx) or HttpRequestException, with jitter.
        public static IAsyncPolicy<float[]> EmbeddingVectorPolicy { get; }
            = CreateEmbeddingPolicy();

        private static IAsyncPolicy<float[]> CreateEmbeddingPolicy()
        {
            var jitterer = Backoff.DecorrelatedJitterBackoffV2(
                TimeSpan.FromMilliseconds(1000),
                5
            );

            return Policy<float[]>
                .Handle<ClientResultException>()
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(
                    jitterer,
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        // Optional: log retries
                    });
        }
    }
}
