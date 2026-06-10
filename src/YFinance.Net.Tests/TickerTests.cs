using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class TickerTests
{
    [Fact]
    public async Task GetQuoteSummaryAsync_DelegatesThroughTickerFacade()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [
                  {
                    "price": {
                      "symbol": "AAPL",
                      "shortName": "Apple",
                      "exchangeName": "NasdaqGS",
                      "currency": "USD",
                      "regularMarketPrice": { "raw": 203.27, "fmt": "203.27" }
                    },
                    "quoteType": {
                      "quoteType": "EQUITY"
                    },
                    "summaryDetail": {}
                  }
                ],
                "error": null
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
          if (request.RequestUri!.Host == "fc.yahoo.com")
          {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
              Content = new StringContent("sad panda", Encoding.UTF8, "text/html")
            };
          }

          if (request.RequestUri.Host == "query1.finance.yahoo.com" && request.RequestUri.AbsolutePath == "/v1/test/getcrumb")
          {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
              Content = new StringContent("crumb-123", Encoding.UTF8, "text/plain")
            };
          }

          return new HttpResponseMessage(HttpStatusCode.OK)
          {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
          };
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var ticker = new Ticker("AAPL", client);

        var result = await ticker.GetQuoteSummaryAsync();

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal("EQUITY", result.QuoteType);
        Assert.Equal(203.27m, result.RegularMarketPrice);
    }

    [Fact]
    public async Task GetNewsAsync_DelegatesThroughTickerFacade()
    {
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "data": {
                        "tickerStream": {
                          "stream": [
                            {
                              "content": {
                                "id": "press-1",
                                "contentType": "STORY",
                                "title": "Apple press release",
                                "pubDate": "2026-06-09T09:00:00Z",
                                "provider": {
                                  "displayName": "Apple"
                                },
                                "canonicalUrl": {
                                  "url": "https://example.com/press-1"
                                }
                              }
                            }
                          ]
                        }
                      }
                    }
                    """, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var ticker = new Ticker("AAPL", client);

        var result = await ticker.GetNewsAsync(tab: TickerNewsTab.PressReleases);

        Assert.NotNull(capturedRequest);
        Assert.Contains("queryRef=pressRelease", capturedRequest!.RequestUri!.Query, StringComparison.Ordinal);
        Assert.Single(result);
        Assert.Equal("Apple press release", result[0].Title);
        Assert.Equal("Apple", result[0].Provider);
    }
}