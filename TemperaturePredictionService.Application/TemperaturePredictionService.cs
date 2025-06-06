using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Application.ModelTraining;

namespace TemperaturePredictionService.Application
{
    public sealed class TemperaturePredictionService : ITemperaturePredictionService
    {
        private readonly IEmbeddingService _embedding;
        private readonly PredictionEngine<TempFeatures, ModelOutput> _engine;
        private readonly ILogger<TemperaturePredictionService> _log;

        public TemperaturePredictionService(
            IEmbeddingService embeddingService,
            ILogger<TemperaturePredictionService> log)
        {
            _embedding = embeddingService;
            _log       = log;

            // Look for the latest trained model in the "experiments" folder
            const string experimentsRoot = "experiments";
            var modelPath = Directory
                .EnumerateDirectories(experimentsRoot)
                .OrderByDescending(Directory.GetCreationTimeUtc)
                .Select(d => Path.Combine(d, "model.zip"))
                .FirstOrDefault(File.Exists)
                ?? throw new FileNotFoundException($"No trained model found in '{experimentsRoot}'.");

            var ml    = new MLContext();
            var model = ml.Model.Load(modelPath, out _);
            _engine   = ml.Model.CreatePredictionEngine<TempFeatures, ModelOutput>(model);

            _log.LogInformation("Temperature model loaded from {ModelPath}", modelPath);
        }

        public async Task<float> PredictTemperatureAsync(
            DateTime date,
            string   city,
            CancellationToken ct = default)
        {
            _log.LogDebug("Predict | City={City} Date={Date:yyyy-MM-dd}", city, date);

            var embedding = await _embedding.GetEmbeddingAsync(city, ct);

            var features = new TempFeatures
            {
                Year          = date.Year,
                Month         = date.Month,
                Day           = date.Day,
                CityEmbedding = embedding
                // Temperature is *not* set for prediction.
            };

            var temp = _engine.Predict(features).Score;

            _log.LogInformation("Predicted {Temp}°C for {City} on {Date:yyyy-MM-dd}", temp, city, date);

            return temp;
        }
    }
}
