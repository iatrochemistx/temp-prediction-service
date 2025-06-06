using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TemperaturePredictionService.Application.ModelTraining;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Core.Models;
using TemperaturePredictionService.Infrastructure.Configuration;
using TemperaturePredictionService.Infrastructure.Data;
using Xunit;

namespace TemperaturePredictionService.Tests.ModelTraining
{
    public class TemperatureModelTrainerIntegrationTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _csvPath;
        private readonly string _outputBase;

        public TemperatureModelTrainerIntegrationTests()
        {
            _tempDir    = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _csvPath    = Path.Combine(_tempDir, "temperatures.csv");
            _outputBase = Path.Combine(_tempDir, "experiments");
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public async Task TrainAsync_CreatesArtifacts_ForValidCsv()
        {
            // Arrange: write a CSV with header + ten rows across various regions
            var lines = new[]
            {
                "Region,Country,City,Month,Day,Year,Temperature",
                "World,USA,Seattle,1,15,2024,5.2",
                "World,USA,New York,2,10,2024,3.8",
                "World,USA,Los Angeles,3,5,2024,15.4",
                "World,Germany,Hamburg,4,20,2024,8.1",
                "World,Germany,Berlin,5,25,2024,12.3",
                "World,Germany,Munich,6,30,2024,18.7",
                "World,France,Paris,7,14,2024,22.5",
                "World,UK,London,8,1,2024,19.0",
                "World,Italy,Rome,9,12,2024,27.2",
                "World,Spain,Madrid,10,3,2024,25.6"
            };
            await File.WriteAllLinesAsync(_csvPath, lines);

            // Configure loader to point at our temp CSV
            var loaderOpts = Options.Create(new CsvLoaderOptions { DefaultCsvPath = _csvPath });
            var loader     = new CsvTemperatureDataLoader(
                NullLogger<CsvTemperatureDataLoader>.Instance,
                loaderOpts
            );

            // Stub embedding service to return a constant 1536-length vector
            const int embeddingSize = 1536;
            var fakeVector          = new float[embeddingSize];
            for (int i = 0; i < embeddingSize; i++) fakeVector[i] = 1.0f;

            var embedMock = new Mock<IEmbeddingService>();
            embedMock
                .Setup(e => e.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeVector);

            // Use our trainer with the injected loader & embedding stub
            var trainer = new TemperatureModelTrainer(
                loader,
                embedMock.Object,
                NullLogger<TemperatureModelTrainer>.Instance,
                outputBase: _outputBase
            );

            // Act
            var summary = await trainer.TrainAsync();

            // Assert: model.zip and metrics.json exist under a timestamped subfolder of _outputBase
            Assert.True(File.Exists(summary.ModelPath));
            Assert.True(File.Exists(summary.MetricsPath));

            // Validate JSON metrics contain non-negative rmse/mae
            var metricsJson = await File.ReadAllTextAsync(summary.MetricsPath);
            using var doc   = JsonDocument.Parse(metricsJson);
            var root        = doc.RootElement;
            Assert.True(root.TryGetProperty("rmse", out var rmseProp));
            Assert.True(root.TryGetProperty("mae", out var maeProp));
            Assert.True(rmseProp.GetDouble() >= 0);
            Assert.True(maeProp.GetDouble() >= 0);
        }

        public void Dispose()
        {
           // try { Directory.Delete(_tempDir, recursive: true); }
           // catch { /* ignore cleanup failures */ }
        }
    }
}
