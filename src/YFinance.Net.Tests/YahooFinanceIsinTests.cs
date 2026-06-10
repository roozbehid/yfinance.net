using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceIsinTests
{
    [Fact]
    public async Task GetIsinAsync_ParsesBusinessInsiderSuggestResponse()
    {
        const string payload = """
            mmSuggestDeliver(0, new Array("Name", "Category", "Keywords", "Bias", "Extension", "IDs"), new Array(new Array("Apple Inc.", "Stocks", "AAPL|US0378331005|AAPL||AAPL", "75", "", "aapl|AAPL|1|869")), 1, 0);
            """;

        HttpRequestMessage? capturedRequest = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "text/plain")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetIsinAsync("AAPL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("markets.businessinsider.com", capturedRequest.RequestUri!.Host);
        Assert.Equal("/ajax/SearchController_Suggest", capturedRequest.RequestUri.AbsolutePath);
        Assert.Contains("max_results=25", capturedRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("query=AAPL", capturedRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Equal("US0378331005", result);
    }

    [Fact]
    public async Task GetIsinAsync_ReturnsNullWhenNoExactMatchExists()
    {
        const string payload = """
            mmSuggestDeliver(0, new Array("Name", "Category", "Keywords", "Bias", "Extension", "IDs"), new Array(new Array("Alphabet Inc Unsponsored Canadian Depository Receipt Hedged", "Stocks", "|CA02080M1005|||", "75", "", "alphabet_3||1|660881670")), 1, 0);
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/plain")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetIsinAsync("GOOGL");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetIsinAsync_ReturnsNullForDashOrIndexSymbols()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not be called")));
        using var client = new YahooFinanceClient(httpClient);

        var dashResult = await client.GetIsinAsync("BRK-B");
        var indexResult = await client.GetIsinAsync("^GSPC");

        Assert.Null(dashResult);
        Assert.Null(indexResult);
    }

    [Fact]
    public async Task TickerFacade_GetIsinAsync_DelegatesThroughClient()
    {
        const string payload = """
            mmSuggestDeliver(0, new Array("Name", "Category", "Keywords", "Bias", "Extension", "IDs"), new Array(new Array("Alphabet A (ex Google)", "Stocks", "GOOGL|US02079K3059|GOOGL||", "75", "", "googl|GOOGL|1|8113479")), 1, 0);
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/plain")
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var ticker = new Ticker("GOOGL", client);

        var result = await ticker.GetIsinAsync();

        Assert.Equal("US02079K3059", result);
    }
}