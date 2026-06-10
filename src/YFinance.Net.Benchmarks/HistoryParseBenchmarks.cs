using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using YFinance.Net;

namespace YFinance.Net.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 8)]
public class HistoryParseBenchmarks
{
    private string _json = string.Empty;
    private byte[] _utf8 = Array.Empty<byte>();

    [Params(252, 756)]
    public int BarCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _json = BuildPayload(BarCount);
        _utf8 = Encoding.UTF8.GetBytes(_json);
    }

    [Benchmark]
    public PriceHistoryResult ParseFromString()
    {
        return PriceHistoryResponseParser.Parse(_json);
    }

    [Benchmark(Baseline = true)]
    public PriceHistoryResult ParseFromUtf8Bytes()
    {
        return PriceHistoryResponseParser.Parse(_utf8);
    }

    private static string BuildPayload(int barCount)
    {
        var start = new DateTimeOffset(2024, 01, 02, 14, 30, 00, TimeSpan.Zero).ToUnixTimeSeconds();
        var timestamps = Enumerable.Range(0, barCount)
            .Select(index => (start + (long)index * 86400).ToString(CultureInfo.InvariantCulture))
            .ToArray();

        var opens = new string[barCount];
        var highs = new string[barCount];
        var lows = new string[barCount];
        var closes = new string[barCount];
        var adjustedCloses = new string[barCount];
        var volumes = new string[barCount];

        for (var index = 0; index < barCount; index++)
        {
            var open = 150m + index * 0.25m;
            var high = open + 3.5m;
            var low = open - 2.75m;
            var close = open + 1.15m;
            var adjustmentRatio = index % 20 == 0 ? 0.98m : 1.0m;
            var adjustedClose = close * adjustmentRatio;
            var volume = 10_000_000L + index * 42_000L;

            opens[index] = open.ToString(CultureInfo.InvariantCulture);
            highs[index] = high.ToString(CultureInfo.InvariantCulture);
            lows[index] = low.ToString(CultureInfo.InvariantCulture);
            closes[index] = close.ToString(CultureInfo.InvariantCulture);
            adjustedCloses[index] = adjustedClose.ToString(CultureInfo.InvariantCulture);
            volumes[index] = volume.ToString(CultureInfo.InvariantCulture);
        }

        var dividends = new StringBuilder();
        for (var index = 40; index < barCount; index += 63)
        {
            if (dividends.Length > 0)
            {
                dividends.Append(',');
            }

            var ts = timestamps[index];
            dividends.Append('"').Append(ts).Append("\":{");
            dividends.Append("\"amount\":0.25,");
            dividends.Append("\"date\":").Append(ts).Append(',');
            dividends.Append("\"currency\":\"USD\"}");
        }

        var splits = new StringBuilder();
        if (barCount > 500)
        {
            var ts = timestamps[500];
          splits.Append('"').Append(ts).Append("\":{");
            splits.Append("\"date\":").Append(ts).Append(',');
            splits.Append("\"numerator\":4,");
            splits.Append("\"denominator\":1}");
        }

        return $$"""
        {
          "chart": {
            "result": [
              {
                "meta": {
                  "currency": "USD",
                  "symbol": "AAPL",
                  "exchangeTimezoneName": "America/New_York",
                  "instrumentType": "EQUITY",
                  "validRanges": ["1d", "5d", "1mo", "3mo", "1y"]
                },
                "timestamp": [{{string.Join(',', timestamps)}}],
                "events": {
                  "dividends": { {{dividends}} },
                  "splits": { {{splits}} }
                },
                "indicators": {
                  "quote": [
                    {
                      "open": [{{string.Join(',', opens)}}],
                      "high": [{{string.Join(',', highs)}}],
                      "low": [{{string.Join(',', lows)}}],
                      "close": [{{string.Join(',', closes)}}],
                      "volume": [{{string.Join(',', volumes)}}]
                    }
                  ],
                  "adjclose": [
                    {
                      "adjclose": [{{string.Join(',', adjustedCloses)}}]
                    }
                  ]
                }
              }
            ],
            "error": null
          }
        }
        """;
    }
}