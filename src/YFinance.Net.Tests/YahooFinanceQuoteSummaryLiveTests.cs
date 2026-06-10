namespace YFinance.Net.Tests;

public sealed class YahooFinanceQuoteSummaryLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetQuoteSummaryAsync_Aapl_ReturnsStableFieldsFromYahoo()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetQuoteSummaryAsync();

        Assert.Equal("AAPL", result.Symbol);
        Assert.False(string.IsNullOrWhiteSpace(result.QuoteType));
        Assert.False(string.IsNullOrWhiteSpace(result.Currency));
        Assert.False(string.IsNullOrWhiteSpace(result.Exchange));
        Assert.True(result.RegularMarketPrice is > 0);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_GetQuoteSummaryAsync_Aapl_ReturnsStableFieldsFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetQuoteSummaryAsync("AAPL");

        Assert.Equal("AAPL", result.Symbol);
        Assert.False(string.IsNullOrWhiteSpace(result.QuoteType));
        Assert.False(string.IsNullOrWhiteSpace(result.Currency));
        Assert.False(string.IsNullOrWhiteSpace(result.Exchange));
        Assert.True(result.RegularMarketPrice is > 0);
    }
}