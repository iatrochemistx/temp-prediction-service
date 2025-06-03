using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Infrastructure.AI;
using TemperaturePredictionService.Infrastructure.Configuration;
using Moq;
using Xunit;      

namespace TemperaturePredictionService.Tests;

public class OpenAiEmbeddingServiceCacheTests
{
    [Fact]
    public async Task GetEmbeddingAsync_Caches_Result_On_Second_Call()
    {
        // ---------- Arrange ----------
        var fakeVector = new[] { 1f, 2f, 3f };

        var adapterMock = new Mock<IEmbeddingClientAdapter>();
        adapterMock
            .Setup(a => a.GenerateAsync("hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeVector);

        var opts  = Options.Create(new OpenAiOptions { CacheDays = 1 });
        var cache = new MemoryCache(new MemoryCacheOptions());

        var service = new OpenAiEmbeddingService(
            adapterMock.Object,
            opts,
            cache,
            NullLogger<OpenAiEmbeddingService>.Instance);

        // ---------- Act ----------
        var first  = await service.GetEmbeddingAsync("hello");
        var second = await service.GetEmbeddingAsync("hello");

        // ---------- Assert ----------
        Assert.Equal(fakeVector, first);
        Assert.Equal(first, second);      // came from cache
        adapterMock.Verify(a => a.GenerateAsync("hello",
                               It.IsAny<CancellationToken>()),
                           Times.Once);   // only one API call
    }
}
