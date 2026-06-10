using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceValuationMeasuresTests
{
    private const string MockHtml = """
        <html><body>
        <table>
            <tr><td></td><td>Current</td><td>12/31/2025</td><td>9/30/2025</td></tr>
            <tr><td>Market Cap</td><td>3.76T</td><td>4.00T</td><td>3.76T</td></tr>
            <tr><td>Enterprise Value</td><td>3.78T</td><td>4.04T</td><td>3.81T</td></tr>
            <tr><td>Trailing P/E</td><td>32.39</td><td>36.44</td><td>38.64</td></tr>
            <tr><td>Forward P/E</td><td>29.76</td><td>32.79</td><td>31.65</td></tr>
            <tr><td>PEG Ratio (5yr expected)</td><td>2.27</td><td>2.75</td><td>2.44</td></tr>
            <tr><td>Price/Sales</td><td>8.77</td><td>9.80</td><td>9.41</td></tr>
            <tr><td>Price/Book</td><td>42.60</td><td>54.21</td><td>57.14</td></tr>
            <tr><td>Enterprise Value/Revenue</td><td>8.68</td><td>9.71</td><td>9.32</td></tr>
            <tr><td>Enterprise Value/EBITDA</td><td>24.73</td><td>27.92</td><td>26.87</td></tr>
        </table>
        </body></html>
        """;

    [Fact]
    public async Task GetValuationMeasuresAsync_ParsesFirstHtmlTable()
    {
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
                Content = new StringContent(MockHtml, Encoding.UTF8, "text/html")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetValuationMeasuresAsync("AAPL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Equal("/quote/AAPL/key-statistics", capturedRequest.RequestUri.AbsolutePath);
        Assert.Contains("p=AAPL", capturedRequest.RequestUri.Query, StringComparison.Ordinal);

        Assert.False(result.IsEmpty);
        Assert.Equal(["Current", "12/31/2025", "9/30/2025"], result.Columns);
        Assert.Equal(9, result.Rows.Length);
        Assert.Equal("Market Cap", result.Rows[0].Metric);
        Assert.Equal("3.76T", result.Rows[0].Values[0]);
        Assert.Equal("32.79", result.Rows[3].Values[1]);
    }

    [Fact]
    public async Task GetValuationMeasuresAsync_ReturnsEmptyWhenNoTableExists()
    {
        const string html = "<html><body><p>No tables here</p></body></html>";

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
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetValuationMeasuresAsync("AAPL");

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public async Task TickerFacade_GetValuationMeasuresAsync_DelegatesThroughClient()
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

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(MockHtml, Encoding.UTF8, "text/html")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var ticker = new Ticker("AAPL", client);

        var result = await ticker.GetValuationMeasuresAsync();

        Assert.False(result.IsEmpty);
        Assert.Equal("Enterprise Value/EBITDA", result.Rows[^1].Metric);
    }
}