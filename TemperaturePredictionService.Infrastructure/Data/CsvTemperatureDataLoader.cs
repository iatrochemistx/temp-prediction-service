using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemperaturePredictionService.Core.Interfaces;
using TemperaturePredictionService.Core.Models;
using TemperaturePredictionService.Infrastructure.Configuration;

namespace TemperaturePredictionService.Infrastructure.Data
{
    /// <summary>
    /// Loads temperature records from a CSV file using CsvHelper. Async streaming, explicit mapping, logs issues.
    /// </summary>
    public sealed class CsvTemperatureDataLoader : ITemperatureDataLoader
    {
        private readonly ILogger<CsvTemperatureDataLoader> _log;
        private readonly CsvLoaderOptions _opts;
        private readonly CsvConfiguration _cfg;

        public CsvTemperatureDataLoader(
            ILogger<CsvTemperatureDataLoader> log,
            IOptions<CsvLoaderOptions> opts)
        {
            _log = log;
            _opts = opts.Value;
            _cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord   = true,
                DetectDelimiter   = true,
                MissingFieldFound = null, // ignore missing fields for now
                BadDataFound = args =>
                    _log.LogWarning(
                        "Bad data '{Field}' in row {Row}: {Record}",
                        args.Field,
                        args.Context.Parser.Row,   // current row number
                        args.RawRecord             // entire problematic record
                    )
            };
        } // <--- this brace was missing

        public async IAsyncEnumerable<TemperatureRecord> LoadRecords(
            string? filePath = null,
            CancellationToken ct = default)
        {
            filePath ??= _opts.DefaultCsvPath;
            if (!File.Exists(filePath))
            {
                _log.LogError("CSV file not found: {Path}", filePath);
                yield break;
            }

            await using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, _cfg);

            // Register class map here
            csv.Context.RegisterClassMap<TemperatureRecordMap>();

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();
                TemperatureRecord? record = null;
                try
                {
                    record = csv.GetRecord<TemperatureRecord>();
                }
                catch (CsvHelperException ex)
                {
                    _log.LogError(ex, "Failed to parse row {Row}", csv.Context.Parser.Row);
                }
                if (record is not null)
                    yield return record;
            }
        }

        // (OPTIONAL: Remove if you switch to all-async)
        public IEnumerable<TemperatureRecord> LoadRecords(string filePath)
        {
            throw new NotImplementedException();
        }

        // Explicit schema mapping
        private sealed class TemperatureRecordMap : ClassMap<TemperatureRecord>
        {
            public TemperatureRecordMap()
            {
                
        Map(m => m.Date).Convert(args =>
        {
            var y = args.Row.GetField<int>("Year");
            var m = args.Row.GetField<int>("Month");
            var d = args.Row.GetField<int>("Day");
            return new DateTime(y, m, d);
        });
        Map(m => m.City).Name("City");
        Map(m => m.Temperature).Name("Temperature");
            }
        }
    }
}
