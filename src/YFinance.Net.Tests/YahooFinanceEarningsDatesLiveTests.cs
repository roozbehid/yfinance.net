namespace YFinance.Net.Tests;

public sealed class YahooFinanceEarningsDatesLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetEarningsDatesAsync_Ibm_ReturnsRequestedPageSize()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetEarningsDatesAsync("IBM", limit: 5);

        Assert.True(result.Total >= 5);
        Assert.Equal(5, result.Entries.Count);
        Assert.All(result.Entries, entry => Assert.NotNull(entry.EarningsDate));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task TickerFacade_GetEarningsDatesAsync_Ibm_ReturnsRequestedPageSize()
    {
        using var ticker = new Ticker("IBM");

        var result = await ticker.GetEarningsDatesAsync(limit: 5);

        Assert.True(result.Total >= 5);
        Assert.Equal(5, result.Entries.Count);
        Assert.All(result.Entries, entry => Assert.NotNull(entry.EarningsDate));
    }
}