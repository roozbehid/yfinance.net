using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinancePriceHistoryTests
{
    [Fact]
    public async Task GetPriceHistoryAsync_ParsesChartResponse()
    {
        const string payload = """
            {
              "chart": {
                "result": [
                  {
                    "meta": {
                      "currency": "USD",
                      "symbol": "AAPL",
                      "exchangeTimezoneName": "America/New_York",
                      "instrumentType": "EQUITY",
                      "validRanges": ["1d", "5d", "1mo"]
                    },
                    "timestamp": [1719878400, 1719964800],
                    "events": {
                      "dividends": {
                        "1719878400": {
                          "amount": 0.25,
                          "date": 1719878400,
                          "currency": "USD"
                        }
                      },
                      "splits": {
                        "1719964800": {
                          "date": 1719964800,
                          "numerator": 4,
                          "denominator": 1
                        }
                      },
                      "capitalGains": {
                        "1719964800": {
                          "date": 1719964800,
                          "amount": 1.5
                        }
                      }
                    },
                    "indicators": {
                      "quote": [
                        {
                          "open": [210.0, 211.0],
                          "high": [212.0, 213.0],
                          "low": [209.5, 210.5],
                          "close": [211.5, 212.5],
                          "volume": [1000, 2000]
                        }
                      ],
                      "adjclose": [
                        {
                          "adjclose": [211.4, 212.4]
                        }
                      ]
                    }
                  }
                ],
                "error": null
              }
            }
            """;

        HttpRequestMessage? capturedRequest = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "AAPL",
            Range = "5d",
            Interval = "1d"
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("/v8/finance/chart/AAPL", capturedRequest.RequestUri.AbsolutePath);
        Assert.Contains("range=5d", capturedRequest.RequestUri.Query);
        Assert.Contains("interval=1d", capturedRequest.RequestUri.Query);
        Assert.Contains("includePrePost=false", capturedRequest.RequestUri.Query);
        Assert.Contains("events=div%2Csplits%2CcapitalGains", capturedRequest.RequestUri.Query);

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("America/New_York", result.ExchangeTimeZone);
        Assert.Equal("EQUITY", result.InstrumentType);
        Assert.Equal(new[] { "1d", "5d", "1mo" }, result.ValidRanges);
        Assert.Equal(2, result.Bars.Length);
        Assert.Equal(211.5m, result.Bars[0].Close);
        Assert.Equal(212.4m, result.Bars[1].AdjustedClose);
        Assert.Equal(2000L, result.Bars[1].Volume);
        Assert.Single(result.Dividends);
        Assert.Equal(0.25m, result.Dividends[0].Amount);
        Assert.Equal("USD", result.Dividends[0].Currency);
        Assert.Single(result.Splits);
        Assert.Equal(4m, result.Splits[0].Ratio);
        Assert.Single(result.CapitalGains);
        Assert.Equal(1.5m, result.CapitalGains[0].Amount);
    }

    [Fact]
    public async Task GetPriceHistoryAsync_AdjustAll_ScalesOhlcAndUsesAdjustedClose()
    {
        const string payload = """
            {
              "chart": {
                "result": [
                  {
                    "meta": {
                      "currency": "USD",
                      "symbol": "AAPL",
                      "exchangeTimezoneName": "America/New_York",
                      "instrumentType": "EQUITY",
                      "validRanges": ["1d"]
                    },
                    "timestamp": [1719878400],
                    "indicators": {
                      "quote": [
                        {
                          "open": [100.0],
                          "high": [120.0],
                          "low": [90.0],
                          "close": [110.0],
                          "volume": [1000]
                        }
                      ],
                      "adjclose": [
                        {
                          "adjclose": [55.0]
                        }
                      ]
                    }
                  }
                ],
                "error": null
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "AAPL",
            Range = "1d",
            Interval = "1d",
            AdjustmentMode = PriceAdjustmentMode.AdjustAll
        });

        Assert.Single(result.Bars);
        Assert.Equal(50m, result.Bars[0].Open);
        Assert.Equal(60m, result.Bars[0].High);
        Assert.Equal(45m, result.Bars[0].Low);
        Assert.Equal(55m, result.Bars[0].Close);
        Assert.Equal(55m, result.Bars[0].AdjustedClose);
    }

    [Fact]
    public async Task GetPriceHistoryAsync_AdjustOpenHighLow_PreservesRawClose()
    {
        const string payload = """
            {
              "chart": {
                "result": [
                  {
                    "meta": {
                      "currency": "USD",
                      "symbol": "AAPL",
                      "exchangeTimezoneName": "America/New_York",
                      "instrumentType": "EQUITY",
                      "validRanges": ["1d"]
                    },
                    "timestamp": [1719878400],
                    "indicators": {
                      "quote": [
                        {
                          "open": [100.0],
                          "high": [120.0],
                          "low": [90.0],
                          "close": [110.0],
                          "volume": [1000]
                        }
                      ],
                      "adjclose": [
                        {
                          "adjclose": [55.0]
                        }
                      ]
                    }
                  }
                ],
                "error": null
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "AAPL",
            Range = "1d",
            Interval = "1d",
            AdjustmentMode = PriceAdjustmentMode.AdjustOpenHighLow
        });

        Assert.Single(result.Bars);
        Assert.Equal(50m, result.Bars[0].Open);
        Assert.Equal(60m, result.Bars[0].High);
        Assert.Equal(45m, result.Bars[0].Low);
        Assert.Equal(110m, result.Bars[0].Close);
        Assert.Equal(55m, result.Bars[0].AdjustedClose);
    }

    [Fact]
    public async Task GetPriceHistoryAsync_ExchangeLocalTimestampMode_ConvertsBarsAndEvents()
    {
        const string payload = """
            {
              "chart": {
                "result": [
                  {
                    "meta": {
                      "currency": "USD",
                      "symbol": "AAPL",
                      "exchangeTimezoneName": "America/New_York",
                      "instrumentType": "EQUITY",
                      "validRanges": ["1d"]
                    },
                    "timestamp": [1719878400],
                    "events": {
                      "dividends": {
                        "1719878400": {
                          "amount": 0.25,
                          "date": 1719878400,
                          "currency": "USD"
                        }
                      }
                    },
                    "indicators": {
                      "quote": [
                        {
                          "open": [100.0],
                          "high": [120.0],
                          "low": [90.0],
                          "close": [110.0],
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

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "AAPL",
            Range = "1d",
            Interval = "1d",
            TimestampMode = PriceTimestampMode.ExchangeLocal
        });

        Assert.Single(result.Bars);
        Assert.NotEqual(TimeSpan.Zero, result.Bars[0].Timestamp.Offset);
        Assert.Single(result.Dividends);
        Assert.NotEqual(TimeSpan.Zero, result.Dividends[0].Timestamp.Offset);
    }

    [Fact]
    public async Task GetPriceHistoriesAsync_PropagatesTimestampModeToBatchResults()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
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
            Symbols = new[] { "AAPL", "MSFT" },
            Range = "5d",
            Interval = "1d",
            TimestampMode = PriceTimestampMode.ExchangeLocal
        });

        Assert.Empty(result.Failures);
        Assert.All(result.Histories.Values, history => Assert.NotEqual(TimeSpan.Zero, history.Bars[0].Timestamp.Offset));
    }

    [Fact]
    public async Task GetPriceHistoryAsync_UsesExplicitPeriodBoundsWhenRangeNotProvided()
    {
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "chart": {
                        "result": [
                          {
                            "meta": {
                              "currency": "USD",
                              "symbol": "AAPL",
                              "exchangeTimezoneName": "America/New_York",
                              "instrumentType": "EQUITY",
                              "validRanges": ["1d"]
                            },
                            "timestamp": [1719878400],
                            "indicators": {
                              "quote": [
                                {
                                  "open": [210.0],
                                  "high": [212.0],
                                  "low": [209.5],
                                  "close": [211.5],
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

        var start = new DateTimeOffset(2024, 07, 01, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 07, 05, 0, 0, 0, TimeSpan.Zero);

        await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
          Symbol = "AAPL",
          Range = null,
          Start = start,
          End = end,
          Interval = "1d"
        });

        Assert.NotNull(capturedRequest);
        Assert.Contains($"period1={start.ToUnixTimeSeconds()}", capturedRequest!.RequestUri!.Query);
        Assert.Contains($"period2={end.ToUnixTimeSeconds()}", capturedRequest.RequestUri.Query);
        Assert.DoesNotContain("range=", capturedRequest.RequestUri.Query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPriceHistoryAsync_ThrowsWhenYahooReturnsChartError()
    {
        const string payload = """
            {
              "chart": {
                "result": [],
                "error": {
                  "code": "Not Found",
                  "description": "No data found"
                }
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "AAPL",
            Range = "5d",
            Interval = "1d"
        }));

        Assert.Contains("returned an error", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPriceHistoryAsync_ReturnsEmptyActionsWhenEventsAreMissing()
    {
        const string payload = """
            {
              "chart": {
                "result": [
                  {
                    "meta": {
                      "currency": "USD",
                      "symbol": "AAPL",
                      "exchangeTimezoneName": "America/New_York",
                      "instrumentType": "EQUITY",
                      "validRanges": ["1d"]
                    },
                    "timestamp": [1719878400],
                    "indicators": {
                      "quote": [
                        {
                          "open": [210.0],
                          "high": [212.0],
                          "low": [209.5],
                          "close": [211.5],
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

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "AAPL",
            Range = "1d",
            Interval = "1d"
        });

        Assert.Empty(result.Dividends);
        Assert.Empty(result.Splits);
        Assert.Empty(result.CapitalGains);
    }

    [Fact]
    public async Task GetPriceHistoryAsync_ThrowsWhenRangeAndDatesAreMixed()
    {
        using var client = new YahooFinanceClient(new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not send request"))));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "AAPL",
            Range = "5d",
            Start = DateTimeOffset.UtcNow.AddDays(-5),
            End = DateTimeOffset.UtcNow,
            Interval = "1d"
        }));

        Assert.Contains("either Range or Start/End", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}