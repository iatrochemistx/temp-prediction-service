using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text.Json;
using Microsoft.ML.Trainers;



namespace TemperaturePredictionService.Application.ModelTraining
{
    public sealed class TemperatureModelTrainer : IModelTrainer
    {
        private readonly ITemperatureDataLoader _loader;
        private readonly IEmbeddingService _embed;
        private readonly ILogger<TemperatureModelTrainer> _log;
        private readonly string _outputBase;

        public TemperatureModelTrainer(
            ITemperatureDataLoader loader,
            IEmbeddingService embed,
            ILogger<TemperatureModelTrainer> log,
            string outputBase = "experiments")
        {
            _loader = loader;
            _embed = embed;
            _log = log;
            _outputBase = outputBase;
        }

        public async Task<ModelTrainingSummary> TrainAsync(CancellationToken ct = default)
        {
            var featureRows = new List<TempFeatures>();
            var embeddingCache = new Dictionary<string, float[]>();

            await foreach (var record in _loader.LoadRecords(ct: ct))
            {
                if (string.IsNullOrWhiteSpace(record.City))
                {
                    _log.LogWarning("Skipping record with empty city");
                    continue;
                }

                if (!embeddingCache.TryGetValue(record.City, out var emb))
                {

                    var embedInput = record.Country is not null
                        ? $"Temperature in {record.City}, {record.Country}"
                        : record.City;
                    emb = await _embed.GetEmbeddingAsync(embedInput, ct);
                    embeddingCache[record.City] = emb;
                }

                featureRows.Add(new TempFeatures
                {
                    Year = record.Date.Year,
                    Month = record.Date.Month,
                    Day = record.Date.Day,
                    CityEmbedding = emb,
                    Temperature = record.Temperature,
                    City = record.City,
                    Country = record.Country,
                    Region = record.Region
                });
            }

            if (featureRows.Count == 0)
            {
                _log.LogWarning("No valid data for training");
                throw new InvalidOperationException("Training aborted: no valid data");
            }

            var ml = new MLContext(seed: 42);
            var dataView = ml.Data.LoadFromEnumerable(featureRows);

            var pipelines = new (string Name, IEstimator<ITransformer> Pipe)[]
{
    ("FastTree", ml.Transforms.Categorical.OneHotEncoding("CityEncoded", "City")
        .Append(ml.Transforms.Categorical.OneHotEncoding("CountryEncoded", "Country"))
        .Append(ml.Transforms.Categorical.OneHotEncoding("RegionEncoded", "Region"))
        .Append(ml.Transforms.ProjectToPrincipalComponents("CityPca", "CityEmbedding", rank: 50))
        .Append(ml.Transforms.Concatenate("Features", "Year", "Month", "Day", "CityPca", "CityEncoded", "CountryEncoded", "RegionEncoded"))
        .Append(ml.Regression.Trainers.FastTree(
            labelColumnName: "Temperature", featureColumnName: "Features",
            numberOfLeaves: 32, numberOfTrees: 200, minimumExampleCountPerLeaf: 10))),
    ("FastForest", ml.Transforms.Categorical.OneHotEncoding("CityEncoded", "City")
        .Append(ml.Transforms.Categorical.OneHotEncoding("CountryEncoded", "Country"))
        .Append(ml.Transforms.Categorical.OneHotEncoding("RegionEncoded", "Region"))
        .Append(ml.Transforms.ProjectToPrincipalComponents("CityPca", "CityEmbedding", rank: 50))
        .Append(ml.Transforms.Concatenate("Features", "Year", "Month", "Day", "CityPca", "CityEncoded", "CountryEncoded", "RegionEncoded"))
        .Append(ml.Regression.Trainers.FastForest(
            labelColumnName: "Temperature", featureColumnName: "Features")))
};


            var split = ml.Data.TrainTestSplit(dataView, testFraction: 0.20, seed: 42);

            (string BestModel, ITransformer Trained, RegressionMetrics Metrics) best = default;
            foreach (var (name, pipe) in pipelines)
            {
                var model = pipe.Fit(split.TrainSet);
                var preds = model.Transform(split.TestSet);
                var metrics = ml.Regression.Evaluate(preds, labelColumnName: "Temperature");

                _log.LogInformation("Model={Model} RMSE={RMSE:F2} MAE={MAE:F2} R2={R2:F2}",
                    name, metrics.RootMeanSquaredError, metrics.MeanAbsoluteError, metrics.RSquared);

                if (best.Trained == null || metrics.RSquared > best.Metrics.RSquared)
                    best = (name, model, metrics);

                // Optionally, save metrics for each model (not just the best)
            }

            // Save only the best model
            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var expDir = Path.Combine(_outputBase, stamp);
            Directory.CreateDirectory(expDir);

            var modelPath = Path.Combine(expDir, $"model_{best.BestModel}.zip");
            var metricsPath = Path.Combine(expDir, $"metrics_{best.BestModel}.json");

            ml.Model.Save(best.Trained, dataView.Schema, modelPath);

            var json = JsonSerializer.Serialize(new
            {
                rmse = best.Metrics.RootMeanSquaredError,
                mae = best.Metrics.MeanAbsoluteError,
                r2 = best.Metrics.RSquared,
                model = best.BestModel
            });
            await File.WriteAllTextAsync(metricsPath, json, ct);

            _log.LogInformation("Best model: {Model} (RMSE={RMSE:F2}, MAE={MAE:F2}, R2={R2:F2})",
                best.BestModel, best.Metrics.RootMeanSquaredError, best.Metrics.MeanAbsoluteError, best.Metrics.RSquared);

            return new ModelTrainingSummary(
                ModelPath: modelPath,
                MetricsPath: metricsPath,
                RMSE: best.Metrics.RootMeanSquaredError,
                MAE: best.Metrics.MeanAbsoluteError,
                ModelType: best.BestModel);

        }
    }
}
