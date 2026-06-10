namespace YFinance.Net.Tests;

public sealed class YahooFinanceAnalystInsightsLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetAnalystInsightsAsync_Aapl_ReturnsRecommendationsAndEarningsAnalytics()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetAnalystInsightsAsync("AAPL");

        Assert.Equal("AAPL", result.Symbol);
        Assert.NotEmpty(result.Recommendations);
        Assert.NotEmpty(result.UpgradesDowngrades);
        Assert.NotEmpty(result.EarningsHistory);
        Assert.NotEmpty(result.EarningsEstimates);
        Assert.NotEmpty(result.RevenueEstimates);
        Assert.NotEmpty(result.EpsTrends);
        Assert.NotEmpty(result.EpsRevisions);
        Assert.NotEmpty(result.GrowthEstimates);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task TickerFacade_GetAnalystInsightsAsync_Aapl_ReturnsRecommendationsAndEarningsAnalytics()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetAnalystInsightsAsync();

        Assert.Equal("AAPL", result.Symbol);
        Assert.NotEmpty(result.Recommendations);
        Assert.NotEmpty(result.UpgradesDowngrades);
        Assert.NotEmpty(result.EarningsHistory);
        Assert.NotEmpty(result.GrowthEstimates);
    }
}