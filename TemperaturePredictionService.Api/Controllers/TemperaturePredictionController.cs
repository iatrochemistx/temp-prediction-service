using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TemperaturePredictionService.Api.Requests;
using TemperaturePredictionService.Core.Interfaces;

namespace TemperaturePredictionService.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TemperaturePredictionController : ControllerBase
    {
        private readonly ITemperaturePredictionService _predictionService;
        private readonly ILogger<TemperaturePredictionController> _log;

        public TemperaturePredictionController(
            ITemperaturePredictionService predictionService,
            ILogger<TemperaturePredictionController> log)
        {
            _predictionService = predictionService;
            _log = log;
        }

        [HttpPost("/predict_temperature")]
        public async Task<ActionResult<PredictResponse>> PredictTemperature(
            [FromBody] PredictRequest request,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.City))
                return BadRequest("City must be provided.");

            try
            {
                var predicted = await _predictionService.PredictTemperatureAsync(request.Date, request.City, ct);
                _log.LogInformation("Prediction for {City} {Date}: {Temp}°C", request.City, request.Date, predicted);
                return Ok(new PredictResponse(predicted));
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Prediction failed for {City} {Date}", request.City, request.Date);
                return StatusCode(500, "Prediction failed: " + ex.Message);
            }
        }
    }
}
