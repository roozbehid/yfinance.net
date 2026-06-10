using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceLookupTests
{
    [Fact]
    public async Task LookupAsync_ParsesYahooLookupResponse()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "documents": [
                      {
                        "symbol": "AAPL",
                        "companyName": "Apple Inc.",
                        "exchange": "NMS",
                        "typeDisp": "Equity",
                        "score": "0.0931",
                        "price": 203.27
                      },
                      {
                        "companyName": "Missing Symbol"
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

        var result = await client.LookupAsync(new LookupRequest
        {
            Query = "AAPL",
            Type = LookupType.Equity,
            Count = 5
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query1.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("query=AAPL", capturedRequest.RequestUri.Query);
        Assert.Contains("type=equity", capturedRequest.RequestUri.Query);
        Assert.Contains("count=5", capturedRequest.RequestUri.Query);
        Assert.Contains("fetchPricingData=true", capturedRequest.RequestUri.Query);
        Assert.True(capturedRequest.Headers.UserAgent.Any());
        Assert.True(capturedRequest.Headers.AcceptLanguage.Any());

        Assert.Single(result.Documents);
        Assert.Equal("AAPL", result.Documents[0].Symbol);
        Assert.Equal("Apple Inc.", result.Documents[0].CompanyName);
        Assert.Equal("Equity", result.Documents[0].Type);
        Assert.Equal(203.27m, result.Documents[0].Price);
    }

    [Fact]
    public async Task LookupAsync_ThrowsWhenYahooReturnsStructuredError()
    {
        const string payload = """
            {
              "finance": {
                "result": [],
                "error": {
                  "code": "Bad Request",
                  "description": "broken"
                }
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.LookupAsync(new LookupRequest
        {
            Query = "AAPL"
        }));

        Assert.Contains("returned an error", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}