using Microsoft.ML.Data;

namespace TemperaturePredictionService.Application.ModelTraining
{
    /// <summary>
    /// DTO for ML.NET model input: numerical date + city embedding + target temperature.
    /// </summary>
    public sealed record TempFeatures
    {
        public float Year { get; init; }
        public float Month { get; init; }
        public float Day { get; init; }
        [VectorType(1536)]
        public float[] CityEmbedding { get; init; } = default!;
        public float Temperature { get; init; } 
    }
}
