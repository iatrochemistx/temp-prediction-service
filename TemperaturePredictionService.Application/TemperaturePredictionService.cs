using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Application.ModelTraining;
using TemperaturePredictionService.Core.Models;

namespace TemperaturePredictionService.Application
{
    public sealed class TemperaturePredictionService : ITemperaturePredictionService
    {
        private readonly IEmbeddingService _embedding;
        private readonly IModelTrainer _trainer;
        private readonly PredictionEngine<TempFeatures, ModelOutput> _engine;
        private readonly ILogger<TemperaturePredictionService> _log;

        public TemperaturePredictionService(
            IEmbeddingService embeddingService,
            IModelTrainer trainer,
            ILogger<TemperaturePredictionService> log)
        {
            _embedding = embeddingService;
            _trainer   = trainer;
            _log       = log;

            const string experimentsRoot = "experiments";
            string modelPath = null;

            // Look for the latest trained model in the "experiments" folder
            if (Directory.Exists(experimentsRoot))
            {
                var latestExpDir = Directory.GetDirectories(experimentsRoot)
                                            .OrderByDescending(d => Directory.GetCreationTimeUtc(d))
                                            .FirstOrDefault();

                if (latestExpDir != null)
                {
                    // Look for any model zip file (model*.zip) in that directory
                    var candidateModels = Directory.GetFiles(latestExpDir, "model*.zip");
                    if (candidateModels.Length > 0)
                    {
                        modelPath = candidateModels[0];
                        _log.LogInformation("Found existing model file: {ModelPath}", modelPath);
                    }
                }
            }

            if (modelPath == null)
            {
                // No model found - train a new one
                _log.LogWarning("No trained model found in '{ExperimentsDir}'. Training a new model...", experimentsRoot);
                ModelTrainingSummary summary = _trainer.TrainAsync().GetAwaiter().GetResult();
                modelPath = summary.ModelPath;
                _log.LogInformation("Trained new model and saved to {ModelPath}", modelPath);
            }

            // Load the model
            var mlContext = new MLContext();
            try
            {
                DataViewSchema schema;
                ITransformer model = mlContext.Model.Load(modelPath, out schema);
                _engine = mlContext.Model.CreatePredictionEngine<TempFeatures, ModelOutput>(model);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to load the model from {ModelPath}", modelPath);
                throw;
            }

            _log.LogInformation("Temperature model loaded and ready from {ModelPath}", modelPath);
        }

        public async Task<float> PredictTemperatureAsync(DateTime date, string city, CancellationToken ct = default)
        {
            _log.LogDebug("Predict | City={City} Date={Date:yyyy-MM-dd}", city, date);

            // Get embedding for the city (vector representation)
            var embedding = await _embedding.GetEmbeddingAsync(city, ct);

            // Prepare feature vector for prediction
            var features = new TempFeatures
            {
                Year          = date.Year,
                Month         = date.Month,
                Day           = date.Day,
                CityEmbedding = embedding
            };

            // Predict temperature using the loaded model
            float prediction = _engine.Predict(features).Score;
            _log.LogInformation("Predicted {Temp}°C for {City} on {Date:yyyy-MM-dd}", prediction, city, date);

            return prediction;
        }
    }
}
