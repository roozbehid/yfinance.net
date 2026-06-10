using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceIsinConvenienceTests
{
    [Fact]
    public async Task GetByIsinAsync_ReturnsTickerAndNewsFromSearch()
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
                }
              ],
              "news": [
                {
                  "uuid": "news-1",
                  "title": "Apple releases something",
                  "publisher": "Example Wire"
                }
              ],
              "lists": [],
              "researchReports": [],
              "nav": []
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

        var result = await client.GetByIsinAsync("US0378331005");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("q=US0378331005", capturedRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("quotesCount=1", capturedRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Equal("US0378331005", result.Isin);
        Assert.NotNull(result.Ticker);
        Assert.Equal("AAPL", result.Ticker!.Value.Symbol);
        Assert.Equal("Apple", result.Ticker.Value.ShortName);
        Assert.Single(result.News);
        Assert.Equal("news-1", result.News[0].Id);
    }

    [Fact]
    public async Task GetByIsinAsync_ThrowsForInvalidIsinFormat()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called")));
        using var client = new YahooFinanceClient(httpClient);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => client.GetByIsinAsync("does_not_exist"));

        Assert.Contains("Invalid ISIN number", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTickerInfoAndNewsByIsinAsync_DelegateThroughAggregateLookup()
    {
        const string payload = """
            {
              "quotes": [
                {
                  "symbol": "FDR.MC",
                  "shortname": "FLUIDRA, S.A.",
                  "longname": "Fluidra, S.A.",
                  "quoteType": "EQUITY",
                  "exchange": "MCE",
                  "exchDisp": "Madrid Stock Exchange CATS"
                }
              ],
              "news": [
                {
                  "uuid": "story-1",
                  "title": "Something happened",
                  "publisher": "Newswire"
                }
              ],
              "lists": [],
              "researchReports": [],
              "nav": []
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var ticker = await client.GetTickerByIsinAsync("ES0137650018");
        var info = await client.GetInfoByIsinAsync("ES0137650018");
        var news = await client.GetNewsByIsinAsync("ES0137650018");

        Assert.Equal("FDR.MC", ticker);
        Assert.NotNull(info);
        Assert.Equal("Fluidra, S.A.", info!.Value.LongName);
        Assert.Single(news);
        Assert.Equal("story-1", news[0].Id);
    }
}