using System.Threading;
using System.Threading.Tasks;
using TemperaturePredictionService.Core.Models;  

namespace TemperaturePredictionService.Core.Interfaces
{
     /// <summary>
    /// Trains a model from historical data and returns a summary of training.
    /// </summary>
    public interface IModelTrainer
    {
        Task<ModelTrainingSummary> TrainAsync(CancellationToken ct = default);
    }
}
