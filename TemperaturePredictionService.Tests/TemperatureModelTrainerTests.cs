using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TemperaturePredictionService.Application.ModelTraining;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Core.Models;
using Xunit;

namespace TemperaturePredictionService.Tests
{
    public class TemperatureModelTrainerTests
    {
        private const int EmbeddingSize = 1536;

        [Fact]
        public async Task TrainAsync_CreatesModelAndMetrics_ForValidData()
        {
            // ---------- arrange ----------
            var records = BuildFakeRecords(10);               
            var loader  = new Mock<ITemperatureDataLoader>();
            loader.Setup(l => l.LoadRecords(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns(FakeAsyncEnumerable(records));

            var fakeEmbedding = Enumerable.Repeat(0.5f, EmbeddingSize).ToArray();
            var embedSvc = new Mock<IEmbeddingService>();
            embedSvc.Setup(e => e.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(fakeEmbedding);

            var trainer = new TemperatureModelTrainer(
                loader.Object,
                embedSvc.Object,
                NullLogger<TemperatureModelTrainer>.Instance);

            // ---------- act ----------
            var summary = await trainer.TrainAsync();

            // ---------- assert ----------
            Assert.True(File.Exists(summary.ModelPath));
            Assert.True(File.Exists(summary.MetricsPath));
            Assert.True(summary.RMSE >= 0);
            Assert.True(summary.MAE  >= 0);
        }

        // Helpers -------------------------------------------------------------

        private static List<TemperatureRecord> BuildFakeRecords(int count)
        {
            var rnd   = new Random(1);
            var list  = new List<TemperatureRecord>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new TemperatureRecord
                {
                    Date        = new DateTime(2024, 1, 1).AddDays(i),
                    City        = $"City{i}",
                    Temperature = 10 + rnd.NextSingle() * 15  
                });
            }
            return list;
        }

        private static async IAsyncEnumerable<TemperatureRecord> FakeAsyncEnumerable(IEnumerable<TemperatureRecord> src)
        {
            foreach (var r in src)
            {
                yield return r;
                await Task.Yield();     
            }
        }
    }
}
