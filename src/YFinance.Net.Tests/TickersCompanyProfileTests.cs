using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class TickersCompanyProfileTests
{
    [Fact]
    public async Task GetCompanyProfilesAsync_DelegatesThroughTickersFacade()
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
        using var tickers = new Tickers(new[] { "AAPL", "MSFT" }, client);

        var result = await tickers.GetCompanyProfilesAsync(maxConcurrency: 2);

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.Profiles.Count);
        Assert.Equal("https://example.com/AAPL", result.Profiles["AAPL"].Website);
        Assert.Equal("https://example.com/MSFT", result.Profiles["MSFT"].Website);
    }
}