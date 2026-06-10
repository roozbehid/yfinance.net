namespace YFinance.Net.Tests;

public sealed class YahooFinanceIsinLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetIsinAsync_Aapl_ReturnsUsIsin()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetIsinAsync("AAPL");

        Assert.Equal("US0378331005", result);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task TickerFacade_GetIsinAsync_Googl_ReturnsUsIsin()
    {
        using var ticker = new Ticker("GOOGL");

        var result = await ticker.GetIsinAsync();

        Assert.Equal("US02079K3059", result);
    }
}