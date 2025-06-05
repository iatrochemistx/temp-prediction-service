namespace TemperaturePredictionService.Core.Models
{
    public record ModelTrainingSummary(
        string ModelPath,
        string MetricsPath,
        double RMSE,
        double MAE,
        string ModelType
    );
}
