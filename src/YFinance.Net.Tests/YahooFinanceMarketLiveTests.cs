namespace YFinance.Net.Tests;

public sealed class YahooFinanceMarketLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task MarketFacade_UsSummaryAndStatus_ReturnsYahooData()
    {
        using var market = new Market(MarketRegion.US);

        var summary = await market.GetSummaryAsync();
        var status = await market.GetStatusAsync();

        AssertValidMarketSummary(summary, MarketRegion.US);
        Assert.True(status.HasValue);
        Assert.Equal("us", status.Value.Id);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task MarketFacade_EuropeStatus_ReturnsNullBecauseYahooIgnoresRegion()
    {
        using var market = new Market(MarketRegion.EUROPE);

        var status = await market.GetStatusAsync();

        Assert.Null(status);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_UsSummaryAndStatus_ReturnsYahooData()
    {
        using var client = new YahooFinanceClient();

        var summary = await client.GetMarketSummaryAsync(MarketRegion.US);
        var status = await client.GetMarketStatusAsync(MarketRegion.US);

        AssertValidMarketSummary(summary, MarketRegion.US);
        Assert.True(status.HasValue);
        Assert.Equal("us", status.Value.Id);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_EuropeStatus_ReturnsNullBecauseYahooIgnoresRegion()
    {
        using var client = new YahooFinanceClient();

        var status = await client.GetMarketStatusAsync(MarketRegion.EUROPE);

        Assert.Null(status);
    }

    private static void AssertValidMarketSummary(MarketSummaryResult summary, MarketRegion expectedRegion)
    {
        Assert.Equal(expectedRegion, summary.Region);
        Assert.NotEmpty(summary.Exchanges);
        Assert.Contains(summary.Exchanges, pair =>
            !string.IsNullOrWhiteSpace(pair.Key) &&
            string.Equals(pair.Key, pair.Value.Exchange, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(summary.Exchanges.Values, entry =>
            !string.IsNullOrWhiteSpace(entry.ShortName) ||
            !string.IsNullOrWhiteSpace(entry.FullExchangeName) ||
            !string.IsNullOrWhiteSpace(entry.Symbol));
    }
}