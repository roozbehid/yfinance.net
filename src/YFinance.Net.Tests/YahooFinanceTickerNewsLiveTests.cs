namespace YFinance.Net.Tests;

public sealed class YahooFinanceTickerNewsLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetNewsAsync_Aapl_ReturnsTickerStreamItems()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetNewsAsync(count: 2);

        Assert.NotEmpty(result);
        Assert.Contains(result, item => !string.IsNullOrWhiteSpace(item.Title));
        Assert.Contains(result, item => item.PublishedAt.HasValue);
        Assert.Contains(result, item =>
            !string.IsNullOrWhiteSpace(item.CanonicalUrl) ||
            !string.IsNullOrWhiteSpace(item.ClickThroughUrl));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetNewsAsync_AaplAllTab_ReturnsTickerStreamItems()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetNewsAsync(count: 2, tab: TickerNewsTab.All);

        Assert.NotEmpty(result);
        Assert.Contains(result, item => !string.IsNullOrWhiteSpace(item.Title));
        Assert.Contains(result, item => item.PublishedAt.HasValue);
    }
}