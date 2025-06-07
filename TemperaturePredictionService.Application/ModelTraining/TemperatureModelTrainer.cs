using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text.Json;

namespace TemperaturePredictionService.Application.ModelTraining
{
    public sealed class TemperatureModelTrainer : IModelTrainer
    {
        private readonly ITemperatureDataLoader           _loader;
        private readonly IEmbeddingService                _embed;
        private readonly ILogger<TemperatureModelTrainer> _log;
        private readonly string                           _outputBase;

        public TemperatureModelTrainer(
            ITemperatureDataLoader           loader,
            IEmbeddingService                embed,
            ILogger<TemperatureModelTrainer> log,
            string outputBase = "experiments")
        {
            _loader     = loader;
            _embed      = embed;
            _log        = log;
            _outputBase = outputBase;
        }

        public async Task<ModelTrainingSummary> TrainAsync(CancellationToken ct = default)
        {
            var featureRows    = new List<TempFeatures>();
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

            var ml        = new MLContext(seed: 42);
            var dataView  = ml.Data.LoadFromEnumerable(featureRows);

            var pipeline = ml.Transforms.Categorical.OneHotEncoding("CityEncoded", "City")
                .Append(ml.Transforms.Categorical.OneHotEncoding("CountryEncoded", "Country"))
                .Append(ml.Transforms.Categorical.OneHotEncoding("RegionEncoded", "Region"))
                .Append(ml.Transforms.ProjectToPrincipalComponents(
                    outputColumnName: "CityPca",
                    inputColumnName: "CityEmbedding",
                    rank: 50))
                .Append(ml.Transforms.Concatenate(
                    "Features",
                    "Year", "Month", "Day",
                    "CityPca", "CityEncoded", "CountryEncoded", "RegionEncoded"))
                .Append(ml.Regression.Trainers.FastTree(
                    labelColumnName: "Temperature",
                    featureColumnName: "Features",
                    numberOfLeaves: 32,
                    numberOfTrees: 200,
                    minimumExampleCountPerLeaf: 10));

            var split     = ml.Data.TrainTestSplit(dataView, testFraction: 0.20, seed: 42);
            var model     = pipeline.Fit(split.TrainSet);

            var preds     = model.Transform(split.TestSet);
            var metrics   = ml.Regression.Evaluate(preds, labelColumnName: "Temperature");

            var stamp     = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var expDir    = Path.Combine(_outputBase, stamp);
            Directory.CreateDirectory(expDir);

            var modelPath   = Path.Combine(expDir, "model.zip");
            var metricsPath = Path.Combine(expDir, "metrics.json");

            ml.Model.Save(model, dataView.Schema, modelPath);

            var json = JsonSerializer.Serialize(new
            {
                rmse = metrics.RootMeanSquaredError,
                mae  = metrics.MeanAbsoluteError,
                r2   = metrics.RSquared
            });
            await File.WriteAllTextAsync(metricsPath, json, ct);

            _log.LogInformation("Training complete – RMSE={RMSE:F2}, MAE={MAE:F2}",
                metrics.RootMeanSquaredError, metrics.MeanAbsoluteError);

            return new ModelTrainingSummary(
                ModelPath:   modelPath,
                MetricsPath: metricsPath,
                RMSE:        metrics.RootMeanSquaredError,
                MAE:         metrics.MeanAbsoluteError,
                ModelType:   "FastTreeRegression");
        }
    }
}
