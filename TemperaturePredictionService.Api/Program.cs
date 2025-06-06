using Microsoft.Extensions.Logging;
using TemperaturePredictionService.Application;
using TemperaturePredictionService.Application.ModelTraining;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Infrastructure.AI;
using TemperaturePredictionService.Infrastructure.Configuration;
using TemperaturePredictionService.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------- options binding ----------
// Bind OpenAI options from configuration into IOptions<OpenAiOptions>
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));

// Bind CSV‐loader options from configuration into IOptions<CsvLoaderOptions>
builder.Services.Configure<CsvLoaderOptions>(builder.Configuration.GetSection("CsvLoader"));

// ---------- DI registrations ----------
builder.Services.AddSingleton<IEmbeddingClientAdapter, OpenAiClientAdapter>();
builder.Services.AddSingleton<ITemperatureDataLoader,       CsvTemperatureDataLoader>();
builder.Services.AddSingleton<IEmbeddingService,            OpenAiEmbeddingService>();
builder.Services.AddSingleton<IModelTrainer,                TemperatureModelTrainer>();
builder.Services.AddSingleton<ITemperaturePredictionService,
                              TemperaturePredictionService.Application.TemperaturePredictionService>();

// ---------- plumbing ----------
builder.Services.AddLogging();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
