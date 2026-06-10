using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceClientTests
{
    [Fact]
    public async Task SearchAsync_ParsesYahooSearchResponse()
    {
        const string payload = """
            {
              "quotes": [
                {
                  "symbol": "AAPL",
                  "shortname": "Apple",
                  "longname": "Apple Inc.",
                  "quoteType": "EQUITY",
                  "exchange": "NMS",
                  "exchDisp": "NasdaqGS"
                },
                {
                  "shortname": "Missing Symbol"
                }
              ],
              "news": [
                {
                  "uuid": "news-1",
                  "title": "Apple releases something",
                  "publisher": "Example Wire"
                }
              ],
              "lists": [{}, {}],
              "researchReports": [{}],
              "nav": [{}, {}, {}]
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

        var result = await client.SearchAsync(new SearchRequest
        {
            Query = "AAPL",
            QuotesCount = 5,
            NewsCount = 1,
            ListsCount = 2,
            IncludeNavigationLinks = true,
            IncludeResearchReports = true
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("https", capturedRequest.RequestUri!.Scheme);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri.Host);
        Assert.Contains("q=AAPL", capturedRequest.RequestUri.Query);
        Assert.Contains("quotesCount=5", capturedRequest.RequestUri.Query);
        Assert.Contains("newsCount=1", capturedRequest.RequestUri.Query);
        Assert.Contains("enableNavLinks=true", capturedRequest.RequestUri.Query);
        Assert.Contains("enableResearchReports=true", capturedRequest.RequestUri.Query);
        Assert.True(capturedRequest.Headers.UserAgent.Any());
        Assert.True(capturedRequest.Headers.AcceptLanguage.Any());
        Assert.Contains(capturedRequest.Headers.Accept, header => header.MediaType == "application/json");

        Assert.Single(result.Quotes);
        Assert.Equal("AAPL", result.Quotes[0].Symbol);
        Assert.Equal("Apple", result.Quotes[0].ShortName);
        Assert.Equal("Apple Inc.", result.Quotes[0].LongName);

        Assert.Single(result.News);
        Assert.Equal("news-1", result.News[0].Id);
        Assert.Equal("Apple releases something", result.News[0].Title);
        Assert.Equal(2, result.ListCount);
        Assert.Equal(1, result.ResearchReportCount);
        Assert.Equal(3, result.NavigationLinkCount);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenYahooIsUnavailable()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Will be right back", Encoding.UTF8, "text/html")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.SearchAsync(new SearchRequest
        {
            Query = "AAPL"
        }));

        Assert.Contains("temporarily unavailable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

}