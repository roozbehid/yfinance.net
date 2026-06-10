using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceMarketTests
{
  [Fact]
  public void Market_StringConstructor_ParsesKnownRegion()
  {
    using var market = new Market("europe");

    Assert.Equal(MarketRegion.EUROPE, market.Region);
  }

  [Fact]
  public void Market_StringConstructor_ThrowsForUnknownRegion()
  {
    var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Market("FR"));

    Assert.Contains("Unknown market region", exception.Message, StringComparison.OrdinalIgnoreCase);
  }

    [Fact]
    public async Task GetMarketSummaryAsync_ParsesExchangeSnapshotRows()
    {
        const string payload = """
            {
              "marketSummaryResponse": {
                "result": [
                  {
                    "exchange": "SNP",
                    "symbol": "^GSPC",
                    "shortName": "S&P 500",
                    "fullExchangeName": "SNP",
                    "quoteType": "INDEX",
                    "marketState": "POST",
                    "exchangeTimezoneName": "America/New_York",
                    "exchangeTimezoneShortName": "EDT",
                    "regularMarketPrice": 7386.65,
                    "regularMarketChange": -19.080078,
                    "regularMarketChangePercent": -0.2576394
                  }
                ],
                "marketCategoryLongName": "US",
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

        var result = await client.GetMarketSummaryAsync(MarketRegion.US);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query1.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("market=US", capturedRequest.RequestUri.Query);
        Assert.Contains("formatted=false", capturedRequest.RequestUri.Query);

        Assert.Equal(MarketRegion.US, result.Region);
        Assert.Equal("US", result.CategoryName);
        Assert.Single(result.Exchanges);
        Assert.True(result.Exchanges.TryGetValue("SNP", out var entry));
        Assert.Equal("^GSPC", entry.Symbol);
        Assert.Equal("S&P 500", entry.ShortName);
        Assert.Equal(7386.65m, entry.RegularMarketPrice);
        Assert.Equal(-19.080078m, entry.RegularMarketChange);
        Assert.Equal(-0.2576394m, entry.RegularMarketChangePercent);
    }

    [Fact]
    public async Task MarketFacade_GetSummaryAsync_DelegatesThroughYahooFinanceClient()
    {
        const string payload = """
            {
              "marketSummaryResponse": {
                "result": [
                  {
                    "exchange": "DJI",
                    "symbol": "^DJI",
                    "shortName": "Dow 30",
                    "regularMarketPrice": 50872.11,
                    "regularMarketChange": 86.09766,
                    "regularMarketChangePercent": 0.16953026
                  }
                ],
                "marketCategoryLongName": "US",
                "error": null
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var market = new Market(MarketRegion.US, client);

        var result = await market.GetSummaryAsync();

        Assert.True(result.Exchanges.TryGetValue("DJI", out var entry));
        Assert.Equal("Dow 30", entry.ShortName);
        Assert.Equal(50872.11m, entry.RegularMarketPrice);
    }

    [Fact]
    public async Task GetMarketStatusAsync_ReturnsNullForUnsupportedNonUsMarketTimeResponses()
    {
        const string payload = """
            {
              "finance": {
                "marketTimes": [
                  {
                    "marketTime": [
                      {
                        "id": "us",
                        "name": "U.S. markets",
                        "status": "closed",
                        "message": "U.S. markets closed",
                        "yfit_market_id": "us_market",
                        "yfit_market_status": "YFT_MARKET_CLOSED",
                        "open": "2026-06-09T13:30:00Z",
                        "close": "2026-06-09T20:00:00Z",
                        "timezone": [
                          {
                            "$text": "America/New_York",
                            "gmtoffset": "-14400",
                            "short": "EDT"
                          }
                        ]
                      }
                    ]
                  }
                ]
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

        var result = await client.GetMarketStatusAsync(MarketRegion.EUROPE);

        Assert.NotNull(capturedRequest);
        Assert.Equal("query1.finance.yahoo.com", capturedRequest!.RequestUri!.Host);
        Assert.Contains("market=EUROPE", capturedRequest.RequestUri.Query);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetMarketStatusAsync_ParsesUsMarketStatus()
    {
        const string payload = """
            {
              "finance": {
                "marketTimes": [
                  {
                    "marketTime": [
                      {
                        "id": "us",
                        "name": "U.S. markets",
                        "status": "closed",
                        "message": "U.S. markets closed",
                        "yfit_market_id": "us_market",
                        "yfit_market_status": "YFT_MARKET_CLOSED",
                        "open": "2026-06-09T13:30:00Z",
                        "close": "2026-06-09T20:00:00Z",
                        "timezone": [
                          {
                            "$text": "America/New_York",
                            "gmtoffset": "-14400",
                            "short": "EDT"
                          }
                        ]
                      }
                    ]
                  }
                ]
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetMarketStatusAsync(MarketRegion.US);

        Assert.True(result.HasValue);
        var status = result.Value;
        Assert.Equal(MarketRegion.US, status.Region);
        Assert.Equal("us", status.Id);
        Assert.Equal("closed", status.Status);
        Assert.Equal("U.S. markets closed", status.Message);
        Assert.Equal(DateTimeOffset.Parse("2026-06-09T13:30:00Z"), status.Open);
        Assert.Equal(DateTimeOffset.Parse("2026-06-09T20:00:00Z"), status.Close);
        Assert.Equal("America/New_York", status.TimezoneName);
        Assert.Equal("EDT", status.TimezoneShortName);
        Assert.Equal(TimeSpan.FromHours(-4), status.UtcOffset);
    }
}