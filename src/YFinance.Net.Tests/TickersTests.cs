using System.Net;
using System.Text;
using System.Text.Json;

namespace YFinance.Net.Tests;

public sealed class TickersTests
{
    [Fact]
    public void Constructor_DeduplicatesSymbolsAndCreatesCaseInsensitiveIndex()
    {
        using var tickers = new Tickers(new[] { "AAPL", "aapl", "MSFT" }, new YahooFinanceClient(new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not send request")))));

        Assert.Equal(new[] { "AAPL", "MSFT" }, tickers.Symbols);
        Assert.Equal(2, tickers.Items.Count);
        Assert.Equal("AAPL", tickers["aapl"].Symbol);
        Assert.Equal("MSFT", tickers["MSFT"].Symbol);
    }

    [Fact]
    public async Task GetHistoriesAsync_DelegatesBatchRequestThroughSharedClient()
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
        using var tickers = new Tickers(new[] { "AAPL", "MSFT" }, client);

        var result = await tickers.GetHistoriesAsync(range: "5d", interval: "1d", maxConcurrency: 2);

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.Histories.Count);
        Assert.Equal("AAPL", result.Histories["AAPL"].Symbol);
        Assert.Equal("MSFT", result.Histories["MSFT"].Symbol);
    }

    [Fact]
    public async Task IndividualTickerFacade_UsesSharedClient()
    {
        var requestCount = 0;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            Interlocked.Increment(ref requestCount);
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
        using var tickers = new Tickers(new[] { "AAPL", "MSFT" }, client);

        var history = await tickers["AAPL"].GetHistoryAsync(range: "5d", interval: "1d");

        Assert.Equal("AAPL", history.Symbol);
        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task GetNewsAsync_DelegatesBatchRequestThroughSharedClient()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("finance.yahoo.com", request.RequestUri!.Host);

            var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            using var document = JsonDocument.Parse(body);
            var serviceConfig = document.RootElement.GetProperty("serviceConfig");
            var count = serviceConfig.GetProperty("snippetCount").GetInt32();
            var symbol = serviceConfig.GetProperty("s")[0].GetString()!;

            Assert.Equal(3, count);

            var payload = $$"""
                {
                  "data": {
                    "tickerStream": {
                      "stream": [
                        {
                          "content": {
                            "id": "{{symbol}}-story-1",
                            "contentType": "STORY",
                            "title": "{{symbol}} news",
                            "pubDate": "2026-06-09T09:30:00Z",
                            "provider": {
                              "displayName": "Unit Test"
                            },
                            "canonicalUrl": {
                              "url": "https://example.com/{{symbol}}"
                            }
                          }
                        }
                      ]
                    }
                  }
                }
                """;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var tickers = new Tickers(new[] { "AAPL", "MSFT" }, client);

        var result = await tickers.GetNewsAsync(count: 3, maxConcurrency: 2);

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.NewsBySymbol.Count);
        Assert.Single(result.NewsBySymbol["AAPL"]);
        Assert.Single(result.NewsBySymbol["MSFT"]);
        Assert.Equal("AAPL news", result.NewsBySymbol["AAPL"][0].Title);
        Assert.Equal("MSFT news", result.NewsBySymbol["MSFT"][0].Title);
    }
}