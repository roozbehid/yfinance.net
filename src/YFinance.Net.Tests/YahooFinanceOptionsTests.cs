using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceOptionsTests
{
    [Fact]
    public async Task GetOptionChainAsync_ParsesExpirationsUnderlyingCallsAndPuts()
    {
        const string payload = """
            {
              "optionChain": {
                "result": [
                  {
                    "expirationDates": [1746144000, 1746748800],
                    "quote": {
                      "symbol": "AAPL",
                      "shortName": "Apple",
                      "longName": "Apple Inc.",
                      "quoteType": "EQUITY",
                      "exchangeName": "NasdaqGS",
                      "currency": "USD",
                      "regularMarketPrice": 203.27,
                      "regularMarketChange": 1.25,
                      "regularMarketChangePercent": 0.61,
                      "regularMarketTime": 1717142400,
                      "marketState": "REGULAR"
                    },
                    "options": [
                      {
                        "expirationDate": 1746144000,
                        "calls": [
                          {
                            "contractSymbol": "AAPL250502C00195000",
                            "lastTradeDate": 1717142400,
                            "strike": 195.0,
                            "lastPrice": 12.4,
                            "bid": 12.1,
                            "ask": 12.7,
                            "change": 0.5,
                            "percentChange": 4.2,
                            "volume": 1200,
                            "openInterest": 4500,
                            "impliedVolatility": 0.255,
                            "inTheMoney": true,
                            "contractSize": "REGULAR",
                            "currency": "USD"
                          }
                        ],
                        "puts": [
                          {
                            "contractSymbol": "AAPL250502P00195000",
                            "lastTradeDate": 1717142400,
                            "strike": 195.0,
                            "lastPrice": 3.2,
                            "bid": 3.1,
                            "ask": 3.3,
                            "change": -0.1,
                            "percentChange": -3.0,
                            "volume": 900,
                            "openInterest": 3200,
                            "impliedVolatility": 0.248,
                            "inTheMoney": false,
                            "contractSize": "REGULAR",
                            "currency": "USD"
                          }
                        ]
                      }
                    ]
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

        var result = await client.GetOptionChainAsync("AAPL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("/v7/finance/options/AAPL", capturedRequest.RequestUri.AbsolutePath);
        Assert.DoesNotContain("date=", capturedRequest.RequestUri.Query, StringComparison.OrdinalIgnoreCase);

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal(new[] { new DateOnly(2025, 05, 02), new DateOnly(2025, 05, 09) }, result.ExpirationDates);
        Assert.Equal(new DateOnly(2025, 05, 02), result.ExpirationDate);
        Assert.NotNull(result.Underlying);
        Assert.Equal("Apple", result.Underlying!.Value.ShortName);
        Assert.Single(result.Calls);
        Assert.Equal("AAPL250502C00195000", result.Calls[0].ContractSymbol);
        Assert.Equal(195.0m, result.Calls[0].Strike);
        Assert.True(result.Calls[0].InTheMoney);
        Assert.Single(result.Puts);
        Assert.Equal("AAPL250502P00195000", result.Puts[0].ContractSymbol);
        Assert.False(result.Puts[0].InTheMoney);
    }

    [Fact]
    public async Task GetOptionExpirationDatesAsync_ReturnsAvailableExpirations()
    {
        const string payload = """
            {
              "optionChain": {
                "result": [
                  {
                    "expirationDates": [1746144000, 1746748800],
                    "quote": { "symbol": "AAPL" },
                    "options": []
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

        var expirations = await client.GetOptionExpirationDatesAsync("AAPL");

        Assert.Equal(new[] { new DateOnly(2025, 05, 02), new DateOnly(2025, 05, 09) }, expirations);
    }

    [Fact]
    public async Task GetOptionChainAsync_ThrowsWhenExpirationDateIsUnavailable()
    {
        const string noResultPayload = """
            {
              "optionChain": {
                "result": [],
                "error": null
              }
            }
            """;

        const string expirationsPayload = """
            {
              "optionChain": {
                "result": [
                  {
                    "expirationDates": [1746144000, 1746748800],
                    "quote": { "symbol": "AAPL" },
                    "options": []
                  }
                ],
                "error": null
              }
            }
            """;

        var optionsRequestCount = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
          if (request.RequestUri!.AbsolutePath.Contains("/v7/finance/options/", StringComparison.OrdinalIgnoreCase))
          {
            optionsRequestCount++;
          }

            var payload = request.RequestUri!.Query.Contains("date=", StringComparison.OrdinalIgnoreCase)
                ? noResultPayload
                : expirationsPayload;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetOptionChainAsync("AAPL", new DateOnly(2025, 05, 10)));

        Assert.Equal(1, optionsRequestCount);
        Assert.Contains("2025-05-10", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2025-05-02", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2025-05-09", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ticker_GetOptionChainAsync_DelegatesThroughTickerFacade()
    {
        const string payload = """
            {
              "optionChain": {
                "result": [
                  {
                    "expirationDates": [1746144000],
                    "quote": { "symbol": "AAPL" },
                    "options": [
                      {
                        "expirationDate": 1746144000,
                        "calls": [ { "contractSymbol": "AAPL250502C00195000" } ],
                        "puts": [ { "contractSymbol": "AAPL250502P00195000" } ]
                      }
                    ]
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
        using var ticker = new Ticker("AAPL", client);

        var result = await ticker.GetOptionChainAsync();

        Assert.Single(result.Calls);
        Assert.Equal("AAPL250502C00195000", result.Calls[0].ContractSymbol);
        Assert.Single(result.Puts);
    }

    [Fact]
    public async Task Ticker_GetOptionExpirationDatesAsync_CachesResultsPerTicker()
    {
        const string payload = """
            {
              "optionChain": {
                "result": [
                  {
                    "expirationDates": [1746144000, 1746748800],
                    "quote": { "symbol": "AAPL" },
                    "options": []
                  }
                ],
                "error": null
              }
            }
            """;

        var optionsRequestCount = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
          if (request.RequestUri!.AbsolutePath.Contains("/v7/finance/options/", StringComparison.OrdinalIgnoreCase))
          {
            optionsRequestCount++;
          }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var ticker = new Ticker("AAPL", client);

        var first = await ticker.GetOptionExpirationDatesAsync();
        var second = await ticker.GetOptionExpirationDatesAsync();

        Assert.Equal(1, optionsRequestCount);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Ticker_GetOptionChainAsync_PrimesExpirationCacheFromDefaultChain()
    {
        const string payload = """
            {
              "optionChain": {
                "result": [
                  {
                    "expirationDates": [1746144000, 1746748800],
                    "quote": { "symbol": "AAPL" },
                    "options": [
                      {
                        "expirationDate": 1746144000,
                        "calls": [ { "contractSymbol": "AAPL250502C00195000" } ],
                        "puts": []
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        var optionsRequestCount = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
          if (request.RequestUri!.AbsolutePath.Contains("/v7/finance/options/", StringComparison.OrdinalIgnoreCase))
          {
            optionsRequestCount++;
          }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var ticker = new Ticker("AAPL", client);

        _ = await ticker.GetOptionChainAsync();
        var expirations = await ticker.GetOptionExpirationDatesAsync();

        Assert.Equal(1, optionsRequestCount);
        Assert.Equal(new[] { new DateOnly(2025, 05, 02), new DateOnly(2025, 05, 09) }, expirations);
    }

    [Fact]
    public async Task Ticker_GetOptionChainAsync_InvalidExpiration_UsesCachedExpirationsWithoutFallbackRequest()
    {
        const string payload = """
            {
              "optionChain": {
                "result": [
                  {
                    "expirationDates": [1746144000, 1746748800],
                    "quote": { "symbol": "AAPL" },
                    "options": []
                  }
                ],
                "error": null
              }
            }
            """;

        var optionsRequestCount = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
          if (request.RequestUri!.AbsolutePath.Contains("/v7/finance/options/", StringComparison.OrdinalIgnoreCase))
          {
            optionsRequestCount++;
          }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var ticker = new Ticker("AAPL", client);

        _ = await ticker.GetOptionExpirationDatesAsync();
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ticker.GetOptionChainAsync(new DateOnly(2025, 05, 10)));

        Assert.Equal(1, optionsRequestCount);
        Assert.Contains("2025-05-10", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

      [Fact]
      public async Task GetOptionExpirationDatesAsync_CacheDisabledInOptions_DoesNotReuseResults()
      {
        const string payload = """
          {
            "optionChain": {
            "result": [
              {
              "expirationDates": [1746144000],
              "quote": { "symbol": "AAPL" },
              "options": []
              }
            ],
            "error": null
            }
          }
          """;

        var optionsRequestCount = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
          if (request.RequestUri!.AbsolutePath.Contains("/v7/finance/options/", StringComparison.OrdinalIgnoreCase))
          {
            optionsRequestCount++;
          }

          return new HttpResponseMessage(HttpStatusCode.OK)
          {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
          };
        }));
        using var client = new YahooFinanceClient(
          httpClient,
          new YahooFinanceClientOptions
          {
            Cache = new YahooFinanceCacheOptions
            {
              EnableOptionExpirationCache = false,
              Store = new MemoryYFinanceCacheStore()
            }
          });

        _ = await client.GetOptionExpirationDatesAsync("AAPL");
        _ = await client.GetOptionExpirationDatesAsync("AAPL");

        Assert.Equal(2, optionsRequestCount);
      }

      [Fact]
      public async Task GetOptionExpirationDatesAsync_RefreshMode_BypassesReadAndUpdatesCache()
      {
        const string refreshedPayload = """
          {
            "optionChain": {
            "result": [
              {
              "expirationDates": [1746748800],
              "quote": { "symbol": "AAPL" },
              "options": []
              }
            ],
            "error": null
            }
          }
          """;

        var cacheStore = new TestCacheStore();
        cacheStore.Set("options-expirations:AAPL", new[] { new DateOnly(2025, 05, 02) }, TimeSpan.FromMinutes(30));

        var optionsRequestCount = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
          if (request.RequestUri!.AbsolutePath.Contains("/v7/finance/options/", StringComparison.OrdinalIgnoreCase))
          {
            optionsRequestCount++;
          }

          return new HttpResponseMessage(HttpStatusCode.OK)
          {
            Content = new StringContent(refreshedPayload, Encoding.UTF8, "application/json")
          };
        }));
        using var client = new YahooFinanceClient(
          httpClient,
          new YahooFinanceClientOptions
          {
            Cache = new YahooFinanceCacheOptions
            {
              Store = cacheStore,
              EnableOptionExpirationCache = true,
              OptionExpirationCacheTtl = TimeSpan.FromMinutes(30)
            }
          });

        var refreshed = await client.GetOptionExpirationDatesAsync("AAPL", YFinanceCacheMode.Refresh);
        var reused = await client.GetOptionExpirationDatesAsync("AAPL");

        Assert.Equal(1, optionsRequestCount);
        Assert.Equal(new[] { new DateOnly(2025, 05, 09) }, refreshed);
        Assert.Equal(refreshed, reused);
      }

      [Fact]
      public async Task GetOptionExpirationDatesAsync_UsesInjectedCacheStore()
      {
        var cacheStore = new TestCacheStore();
        cacheStore.Set("options-expirations:AAPL", new[] { new DateOnly(2025, 05, 02) }, TimeSpan.FromMinutes(30));

        var optionsRequestCount = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
          if (request.RequestUri!.AbsolutePath.Contains("/v7/finance/options/", StringComparison.OrdinalIgnoreCase))
          {
            optionsRequestCount++;
          }

          return new HttpResponseMessage(HttpStatusCode.OK)
          {
            Content = new StringContent("should not be used", Encoding.UTF8, "text/plain")
          };
        }));
        using var client = new YahooFinanceClient(
          httpClient,
          new YahooFinanceClientOptions
          {
            Cache = new YahooFinanceCacheOptions
            {
              Store = cacheStore,
              EnableOptionExpirationCache = true,
              OptionExpirationCacheTtl = TimeSpan.FromMinutes(30)
            }
          });

        var expirations = await client.GetOptionExpirationDatesAsync("AAPL");

        Assert.Equal(0, optionsRequestCount);
        Assert.Equal(new[] { new DateOnly(2025, 05, 02) }, expirations);
        Assert.True(cacheStore.TryGetCalls > 0);
      }

      private sealed class TestCacheStore : IYFinanceCacheStore
      {
        private readonly Dictionary<string, object?> _entries = new(StringComparer.Ordinal);

        public int TryGetCalls { get; private set; }

        public bool TryGetValue<T>(string key, out T? value)
        {
          TryGetCalls++;
          if (_entries.TryGetValue(key, out var stored) && stored is T typed)
          {
            value = typed;
            return true;
          }

          value = default;
          return false;
        }

        public void Set<T>(string key, T value, TimeSpan? ttl = null)
        {
          _entries[key] = value;
        }

        public void Remove(string key)
        {
          _entries.Remove(key);
        }
      }
}