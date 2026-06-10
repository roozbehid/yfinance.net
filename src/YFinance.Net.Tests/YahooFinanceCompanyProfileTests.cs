using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceCompanyProfileTests
{
    [Fact]
    public async Task GetCompanyProfileAsync_ParsesFundamentalsAndProfileFields()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [
                  {
                    "price": {
                      "symbol": "AAPL",
                      "shortName": "Apple",
                      "longName": "Apple Inc.",
                      "exchangeName": "NasdaqGS",
                      "currency": "USD",
                      "marketCap": { "raw": 3000000000000, "fmt": "3T" },
                      "regularMarketPrice": { "raw": 203.27, "fmt": "203.27" }
                    },
                    "quoteType": {
                      "quoteType": "EQUITY"
                    },
                    "summaryDetail": {
                      "trailingPE": { "raw": 31.5, "fmt": "31.5" },
                      "forwardPE": { "raw": 28.2, "fmt": "28.2" },
                      "dividendYield": { "raw": 0.0045, "fmt": "0.45%" },
                      "beta": { "raw": 1.1, "fmt": "1.1" }
                    },
                    "assetProfile": {
                      "sector": "Technology",
                      "industry": "Consumer Electronics",
                      "website": "https://www.apple.com",
                      "irWebsite": "https://investor.apple.com",
                      "phone": "408-996-1010",
                      "address1": "One Apple Park Way",
                      "city": "Cupertino",
                      "state": "CA",
                      "zip": "95014",
                      "country": "United States",
                      "longBusinessSummary": "Apple designs consumer electronics.",
                      "fullTimeEmployees": 161000,
                      "companyOfficers": [
                        {
                          "name": "Tim Cook",
                          "title": "CEO",
                          "age": 63,
                          "yearBorn": 1960,
                          "totalPay": { "raw": 1000000, "fmt": "1M" }
                        }
                      ]
                    },
                    "financialData": {
                      "currentPrice": { "raw": 203.27, "fmt": "203.27" },
                      "earningsGrowth": { "raw": 0.12, "fmt": "12%" },
                      "revenueGrowth": { "raw": 0.08, "fmt": "8%" },
                      "profitMargins": { "raw": 0.25, "fmt": "25%" },
                      "grossMargins": { "raw": 0.43, "fmt": "43%" },
                      "operatingMargins": { "raw": 0.31, "fmt": "31%" },
                      "returnOnAssets": { "raw": 0.17, "fmt": "17%" },
                      "returnOnEquity": { "raw": 1.45, "fmt": "145%" }
                    },
                    "defaultKeyStatistics": {
                      "enterpriseValue": { "raw": 3100000000000, "fmt": "3.1T" }
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

            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetCompanyProfileAsync("AAPL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("modules=price%2CquoteType%2CsummaryDetail%2CassetProfile%2CfinancialData%2CdefaultKeyStatistics", capturedRequest.RequestUri.Query);
        Assert.Contains("crumb=crumb-123", capturedRequest.RequestUri.Query);

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal("Apple", result.ShortName);
        Assert.Equal("Apple Inc.", result.LongName);
        Assert.Equal("EQUITY", result.QuoteType);
        Assert.Equal("NasdaqGS", result.Exchange);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(3000000000000m, result.MarketCap);
        Assert.Equal(3100000000000m, result.EnterpriseValue);
        Assert.Equal(31.5m, result.TrailingPe);
        Assert.Equal(28.2m, result.ForwardPe);
        Assert.Equal(0.0045m, result.DividendYield);
        Assert.Equal(1.1m, result.Beta);
        Assert.Equal(203.27m, result.CurrentPrice);
        Assert.Equal("Technology", result.Sector);
        Assert.Equal("Consumer Electronics", result.Industry);
        Assert.Equal("https://www.apple.com", result.Website);
        Assert.Equal("Apple designs consumer electronics.", result.LongBusinessSummary);
        Assert.Equal(161000, result.FullTimeEmployees);
        Assert.Single(result.Officers);
        Assert.Equal("Tim Cook", result.Officers[0].Name);
        Assert.Equal("CEO", result.Officers[0].Title);
        Assert.Equal(1000000m, result.Officers[0].TotalPay);
    }

    [Fact]
    public async Task GetCompanyProfileAsync_ThrowsWhenYahooReturnsStructuredError()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [],
                "error": {
                  "code": "Not Found",
                  "description": "broken"
                }
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

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetCompanyProfileAsync("AAPL"));

        Assert.Contains("returned an error", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}