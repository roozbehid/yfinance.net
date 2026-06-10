using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceBatchPriceHistoryTests
{
    [Fact]
    public async Task GetPriceHistoriesAsync_ReturnsSeparatedResultsPerSymbol()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            var symbol = request.RequestUri!.AbsolutePath.Split('/').Last();
            var payload = $$"""
                {
                  "chart": {
                    "result": [
                      {
                        "meta": {
                          "currency": "USD",
                          "symbol": "{{symbol}}",
                          "exchangeTimezoneName": "America/New_York",
                          "instrumentType": "EQUITY",
                          "validRanges": ["5d"]
                        },
                        "timestamp": [1719878400],
                        "indicators": {
                          "quote": [
                            {
                              "open": [100.0],
                              "high": [120.0],
                              "low": [90.0],
                              "close": [{{(symbol == "AAPL" ? "111.5" : "222.5")}}],
                              "volume": [1000]
                            }
                          ]
                        }
                      }
                    ],
                    "error": null
                  }
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetPriceHistoriesAsync(new BatchPriceHistoryRequest
        {
            Symbols = new[] { "AAPL", "MSFT" },
            Range = "5d",
            Interval = "1d",
            MaxConcurrency = 2
        });

        Assert.Equal(2, result.Histories.Count);
        Assert.Empty(result.Failures);
        Assert.Equal("AAPL", result.Histories["AAPL"].Symbol);
        Assert.Equal(111.5m, result.Histories["AAPL"].Bars[0].Close);
        Assert.Equal("MSFT", result.Histories["MSFT"].Symbol);
        Assert.Equal(222.5m, result.Histories["MSFT"].Bars[0].Close);
    }

    [Fact]
    public async Task GetPriceHistoriesAsync_CollectsPartialFailuresWithoutFailingWholeBatch()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            var symbol = request.RequestUri!.AbsolutePath.Split('/').Last();
            if (string.Equals(symbol, "BAD", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {
                          "chart": {
                            "result": [],
                            "error": {
                              "code": "Not Found",
                              "description": "No data found"
                            }
                          }
                        }
                        """, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""
                    {
                      "chart": {
                        "result": [
                          {
                            "meta": {
                              "currency": "USD",
                              "symbol": "{{symbol}}",
                              "exchangeTimezoneName": "America/New_York",
                              "instrumentType": "EQUITY",
                              "validRanges": ["5d"]
                            },
                            "timestamp": [1719878400],
                            "indicators": {
                              "quote": [
                                {
                                  "open": [100.0],
                                  "high": [120.0],
                                  "low": [90.0],
                                  "close": [111.5],
                                  "volume": [1000]
                                }
                              ]
                            }
                          }
                        ],
                        "error": null
                      }
                    }
                    """, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetPriceHistoriesAsync(new BatchPriceHistoryRequest
        {
            Symbols = new[] { "AAPL", "BAD" },
            Range = "5d",
            Interval = "1d"
        });

        Assert.Single(result.Histories);
        Assert.True(result.Histories.ContainsKey("AAPL"));
        Assert.Single(result.Failures);
        Assert.True(result.Failures.ContainsKey("BAD"));
        Assert.Contains("returned an error", result.Failures["BAD"].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPriceHistoriesAsync_DeduplicatesSymbolsCaseInsensitively()
    {
        var callCount = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            Interlocked.Increment(ref callCount);
            var symbol = request.RequestUri!.AbsolutePath.Split('/').Last();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""
                    {
                      "chart": {
                        "result": [
                          {
                            "meta": {
                              "currency": "USD",
                              "symbol": "{{symbol.ToUpperInvariant()}}",
                              "exchangeTimezoneName": "America/New_York",
                              "instrumentType": "EQUITY",
                              "validRanges": ["5d"]
                            },
                            "timestamp": [1719878400],
                            "indicators": {
                              "quote": [
                                {
                                  "open": [100.0],
                                  "high": [120.0],
                                  "low": [90.0],
                                  "close": [111.5],
                                  "volume": [1000]
                                }
                              ]
                            }
                          }
                        ],
                        "error": null
                      }
                    }
                    """, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetPriceHistoriesAsync(new BatchPriceHistoryRequest
        {
            Symbols = new[] { "AAPL", "aapl", "Aapl" },
            Range = "5d",
            Interval = "1d"
        });

        Assert.Single(result.Histories);
        Assert.Empty(result.Failures);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetPriceHistoriesAsync_ThrowsWhenMaxConcurrencyIsInvalid()
    {
        using var client = new YahooFinanceClient(new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not send request"))));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetPriceHistoriesAsync(new BatchPriceHistoryRequest
        {
            Symbols = new[] { "AAPL" },
            Range = "5d",
            Interval = "1d",
            MaxConcurrency = 0
        }));
    }
}