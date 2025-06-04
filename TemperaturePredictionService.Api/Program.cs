using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Infrastructure.AI;
using TemperaturePredictionService.Infrastructure.Configuration;
using TemperaturePredictionService.Infrastructure.Resilience;

var builder = WebApplication.CreateBuilder(args);

// --- Config/options
builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection("OpenAiOptions"));

// --- Infrastructure
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(RetryPolicies.EmbeddingVectorPolicy);
builder.Services.AddSingleton<IEmbeddingClientAdapter, OpenAiClientAdapter>();
builder.Services.AddSingleton<IEmbeddingService, OpenAiEmbeddingService>();

// --- ASP.NET Core
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
