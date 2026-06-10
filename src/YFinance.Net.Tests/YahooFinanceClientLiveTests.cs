namespace YFinance.Net.Tests;

public sealed class YahooFinanceClientLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task SearchAsync_Aapl_ReturnsMatchingQuoteFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.SearchAsync(new SearchRequest
        {
            Query = "AAPL",
            QuotesCount = 5,
            NewsCount = 1
        });

        Assert.NotEmpty(result.Quotes);
        Assert.Contains(result.Quotes, quote => string.Equals(quote.Symbol, "AAPL", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetNewsAsync_Aapl_ReturnsTickerNewsFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetNewsAsync("AAPL", count: 2);

        Assert.NotEmpty(result);
        Assert.Contains(result, item => !string.IsNullOrWhiteSpace(item.Title));
        Assert.Contains(result, item => item.PublishedAt.HasValue);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetNewsAsync_AaplAndMsft_ReturnsBatchTickerNewsFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetNewsAsync(new BatchTickerNewsRequest(new[] { "AAPL", "MSFT" }, Count: 1, MaxConcurrency: 2));

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.NewsBySymbol.Count);
        Assert.NotEmpty(result.NewsBySymbol["AAPL"]);
        Assert.NotEmpty(result.NewsBySymbol["MSFT"]);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetNewsAsync_IbmPressReleases_ReturnsOrGracefullyHandlesTickerPressReleaseFeed()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetNewsAsync("IBM", count: 2, tab: TickerNewsTab.PressReleases);

        Assert.NotNull(result);
        if (result.Length > 0)
        {
            Assert.Contains(result, item => !string.IsNullOrWhiteSpace(item.Title));
            Assert.Contains(result, item =>
                !string.IsNullOrWhiteSpace(item.CanonicalUrl) ||
                !string.IsNullOrWhiteSpace(item.ClickThroughUrl));
        }
    }
}