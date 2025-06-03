using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TemperaturePredictionService.Core.Models;
using TemperaturePredictionService.Infrastructure.Configuration;
using TemperaturePredictionService.Infrastructure.Data;
using Xunit;

namespace TemperaturePredictionService.Tests.Infrastructure;

/// <summary>
/// Unit tests for CsvTemperatureDataLoader/>.
/// </summary>
public sealed class CsvTemperatureDataLoaderTests{

    private static CsvTemperatureDataLoader CreateLoader(string path) =>
        new(
            NullLogger<CsvTemperatureDataLoader>.Instance,
            Options.Create(new CsvLoaderOptions { DefaultCsvPath = path }));

    [Theory]
    [InlineData("Berlin",  "Germany", 2024,  1, 15,  2.7f)]
    [InlineData("Rome",    "Italy",   2024,  6, 20, 25.1f)]
    [InlineData("Oslo",    "Norway",  2023, 12,  5, -3.4f)]
    [InlineData("Madrid",  "Spain",   2025,  8, 10, 33.2f)]
    [InlineData("Toronto", "Canada",  2024,  3, 22,  5.6f)]
    public async Task LoadRecords_ShouldParseSingleRow(
        string city,
        string country,
        int    year,
        int    month,
        int    day,
        float  temp,
        CancellationToken ct = default)
    {
        // ---------- Arrange ----------
        var csv = new StringBuilder()
            .AppendLine("Region,Country,City,Month,Day,Year,Temperature")
            .AppendLine($"World,{country},{city},{month},{day},{year},{temp}")
            .ToString();

        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, csv, ct);

        try
        {
            var loader  = CreateLoader(tmp);
            var results = new List<TemperatureRecord>();

            // ---------- Act ----------
            await foreach (var rec in loader.LoadRecords(ct: ct))
                results.Add(rec);

            // ---------- Assert ----------
            results.Should().ContainSingle();

            var record = results[0];
            record.City.Should().Be(city);
            record.Date.Should().Be(new(year, month, day));
            record.Temperature.Should().BeApproximately(temp, 0.001f);
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}
