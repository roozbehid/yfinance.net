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

        Assert.NotEmpty(summary.Exchanges);
        Assert.Contains(summary.Exchanges.Keys, exchange => string.Equals(exchange, "SNP", StringComparison.OrdinalIgnoreCase));
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

        Assert.NotEmpty(summary.Exchanges);
        Assert.Contains(summary.Exchanges.Keys, exchange => string.Equals(exchange, "SNP", StringComparison.OrdinalIgnoreCase));
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
}