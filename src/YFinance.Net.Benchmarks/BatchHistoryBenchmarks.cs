using System.Net;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using YFinance.Net;

namespace YFinance.Net.Benchmarks;

[MemoryDiagnoser]
[MarkdownExporter]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 8)]
public class BatchHistoryBenchmarks
{
    private YahooFinanceClient _client = null!;

    [Params(8, 32)]
    public int SymbolCount { get; set; }

    [Params(1, 4)]
    public int MaxConcurrency { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var payload = Encoding.UTF8.GetBytes(BuildPayload(252));
        var httpClient = new HttpClient(new StaticJsonHttpMessageHandler(payload));
        _client = new YahooFinanceClient(httpClient);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _client.Dispose();
    }

    [Benchmark]
    public Task<BatchPriceHistoryResult> GetPriceHistoriesAsync()
    {
      return _client.GetPriceHistoriesAsync(new BatchPriceHistoryRequest
      {
        Symbols = BuildSymbols(SymbolCount),
        Range = "1mo",
        Interval = "1d",
        MaxConcurrency = MaxConcurrency
      });
    }

    private static IReadOnlyList<string> BuildSymbols(int symbolCount)
    {
        var symbols = new string[symbolCount];
        for (var index = 0; index < symbolCount; index++)
        {
            symbols[index] = $"SYM{index:D3}";
        }

        return symbols;
    }

    private static string BuildPayload(int barCount)
    {
        var start = new DateTimeOffset(2024, 01, 02, 14, 30, 00, TimeSpan.Zero).ToUnixTimeSeconds();
        var timestamps = Enumerable.Range(0, barCount)
            .Select(index => (start + (long)index * 86400).ToString(System.Globalization.CultureInfo.InvariantCulture))
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

            opens[index] = open.ToString(System.Globalization.CultureInfo.InvariantCulture);
            highs[index] = high.ToString(System.Globalization.CultureInfo.InvariantCulture);
            lows[index] = low.ToString(System.Globalization.CultureInfo.InvariantCulture);
            closes[index] = close.ToString(System.Globalization.CultureInfo.InvariantCulture);
            adjustedCloses[index] = adjustedClose.ToString(System.Globalization.CultureInfo.InvariantCulture);
            volumes[index] = volume.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return $$"""
        {
          "chart": {
            "result": [
              {
                "meta": {
                  "currency": "USD",
                  "symbol": "BENCH",
                  "exchangeTimezoneName": "America/New_York",
                  "instrumentType": "EQUITY",
                  "validRanges": ["1d", "5d", "1mo", "3mo", "1y"]
                },
                "timestamp": [{{string.Join(',', timestamps)}}],
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

    private sealed class StaticJsonHttpMessageHandler(byte[] payload) : HttpMessageHandler
    {
        private readonly byte[] _payload = payload;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_payload)
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return Task.FromResult(response);
        }
    }
}