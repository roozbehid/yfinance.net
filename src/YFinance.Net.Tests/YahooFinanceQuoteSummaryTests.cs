using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceQuoteSummaryTests
{
    [Fact]
    public async Task GetQuoteSummaryAsync_ParsesStableQuoteFields()
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
                      "regularMarketPrice": { "raw": 203.27, "fmt": "203.27" }
                    },
                    "quoteType": {
                      "quoteType": "EQUITY"
                    },
                    "summaryDetail": {
                      "previousClose": { "raw": 201.14, "fmt": "201.14" },
                      "open": { "raw": 202.00, "fmt": "202.00" },
                      "dayHigh": { "raw": 204.40, "fmt": "204.40" },
                      "dayLow": { "raw": 200.80, "fmt": "200.80" },
                      "volume": { "raw": 123456789, "fmt": "123.46M" }
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

        var result = await client.GetQuoteSummaryAsync("AAPL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("/v10/finance/quoteSummary/AAPL", capturedRequest.RequestUri.AbsolutePath);
        Assert.Contains("modules=price%2CquoteType%2CsummaryDetail", capturedRequest.RequestUri.Query);
        Assert.Contains("symbol=AAPL", capturedRequest.RequestUri.Query);
        Assert.Contains("crumb=crumb-123", capturedRequest.RequestUri.Query);

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal("Apple", result.ShortName);
        Assert.Equal("Apple Inc.", result.LongName);
        Assert.Equal("EQUITY", result.QuoteType);
        Assert.Equal("NasdaqGS", result.Exchange);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(203.27m, result.RegularMarketPrice);
        Assert.Equal(201.14m, result.PreviousClose);
        Assert.Equal(202.00m, result.Open);
        Assert.Equal(204.40m, result.DayHigh);
        Assert.Equal(200.80m, result.DayLow);
        Assert.Equal(123456789L, result.Volume);
    }

    [Fact]
    public async Task GetQuoteSummaryAsync_ThrowsWhenYahooReturnsStructuredError()
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

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetQuoteSummaryAsync("AAPL"));

        Assert.Contains("returned an error", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}