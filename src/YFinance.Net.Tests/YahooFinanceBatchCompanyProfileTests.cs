using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceBatchCompanyProfileTests
{
    [Fact]
    public async Task GetCompanyProfilesAsync_ReturnsSeparatedProfilesPerSymbol()
    {
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

            var symbol = request.RequestUri.AbsolutePath.Split('/').Last();
            var payload = $$"""
                {
                  "quoteSummary": {
                    "result": [
                      {
                        "price": {
                          "symbol": "{{symbol}}",
                          "shortName": "{{symbol}} short",
                          "longName": "{{symbol}} long",
                          "exchangeName": "NasdaqGS",
                          "currency": "USD",
                          "marketCap": { "raw": 3000000000000, "fmt": "3T" },
                          "regularMarketPrice": { "raw": 203.27, "fmt": "203.27" }
                        },
                        "quoteType": {
                          "quoteType": "EQUITY"
                        },
                        "assetProfile": {
                          "sector": "Technology",
                          "industry": "Consumer Electronics",
                          "website": "https://example.com/{{symbol}}",
                          "longBusinessSummary": "{{symbol}} summary"
                        },
                        "financialData": {
                          "currentPrice": { "raw": 203.27, "fmt": "203.27" }
                        },
                        "defaultKeyStatistics": {}
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

        var result = await client.GetCompanyProfilesAsync(new BatchCompanyProfileRequest(new[] { "AAPL", "MSFT" }, 2));

        Assert.Equal(2, result.Profiles.Count);
        Assert.Empty(result.Failures);
        Assert.Equal("AAPL", result.Profiles["AAPL"].Symbol);
        Assert.Equal("https://example.com/AAPL", result.Profiles["AAPL"].Website);
        Assert.Equal("MSFT", result.Profiles["MSFT"].Symbol);
        Assert.Equal("https://example.com/MSFT", result.Profiles["MSFT"].Website);
    }

    [Fact]
    public async Task GetCompanyProfilesAsync_CollectsPartialFailuresWithoutFailingWholeBatch()
    {
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

            var symbol = request.RequestUri.AbsolutePath.Split('/').Last();
            if (string.Equals(symbol, "BAD", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {
                          "quoteSummary": {
                            "result": [],
                            "error": {
                              "code": "Not Found",
                              "description": "broken"
                            }
                          }
                        }
                        """, Encoding.UTF8, "application/json")
                };
            }

            var payload = $$"""
                {
                  "quoteSummary": {
                    "result": [
                      {
                        "price": {
                          "symbol": "{{symbol}}",
                          "shortName": "{{symbol}} short",
                          "longName": "{{symbol}} long",
                          "exchangeName": "NasdaqGS",
                          "currency": "USD"
                        },
                        "quoteType": {
                          "quoteType": "EQUITY"
                        },
                        "assetProfile": {
                          "website": "https://example.com/{{symbol}}"
                        },
                        "financialData": {},
                        "defaultKeyStatistics": {}
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

        var result = await client.GetCompanyProfilesAsync(new BatchCompanyProfileRequest(new[] { "AAPL", "BAD" }, 2));

        Assert.Single(result.Profiles);
        Assert.True(result.Profiles.ContainsKey("AAPL"));
        Assert.Single(result.Failures);
        Assert.True(result.Failures.ContainsKey("BAD"));
        Assert.Contains("returned an error", result.Failures["BAD"].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCompanyProfilesAsync_DeduplicatesSymbolsCaseInsensitively()
    {
        var callCount = 0;
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

            if (request.RequestUri!.AbsolutePath.Contains("quoteSummary", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref callCount);
            }

            var symbol = request.RequestUri.AbsolutePath.Split('/').Last();
            var payload = $$"""
                {
                  "quoteSummary": {
                    "result": [
                      {
                        "price": {
                          "symbol": "{{symbol.ToUpperInvariant()}}",
                          "shortName": "{{symbol}} short",
                          "longName": "{{symbol}} long",
                          "exchangeName": "NasdaqGS",
                          "currency": "USD"
                        },
                        "quoteType": {
                          "quoteType": "EQUITY"
                        },
                        "assetProfile": {},
                        "financialData": {},
                        "defaultKeyStatistics": {}
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

        var result = await client.GetCompanyProfilesAsync(new BatchCompanyProfileRequest(new[] { "AAPL", "aapl", "Aapl" }, 3));

        Assert.Single(result.Profiles);
        Assert.Empty(result.Failures);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetCompanyProfilesAsync_ThrowsWhenMaxConcurrencyIsInvalid()
    {
        using var client = new YahooFinanceClient(new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not send request"))));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetCompanyProfilesAsync(new BatchCompanyProfileRequest(new[] { "AAPL" }, 0)));
    }
}