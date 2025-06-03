using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Infrastructure.AI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IEmbeddingClientAdapter, OpenAiClientAdapter>();
builder.Services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();

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
