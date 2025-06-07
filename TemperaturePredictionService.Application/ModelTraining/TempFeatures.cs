using Microsoft.ML.Data;

namespace TemperaturePredictionService.Application.ModelTraining
{
    public class TempFeatures
    {
        public float Year { get; set; }
        public float Month { get; set; }
        public float Day { get; set; }
        [VectorType(1536)]
        public float[] CityEmbedding { get; set; }
        public float Temperature { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Region { get; set; }
    }
}
