namespace YFinance.Net.Tests;

public sealed class YahooFinanceCompanyProfileLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetCompanyProfileAsync_Aapl_ReturnsFundamentalsAndDetailsFromYahoo()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetCompanyProfileAsync();

        Assert.Equal("AAPL", result.Symbol);
        Assert.True(result.MarketCap is > 0);
        Assert.False(string.IsNullOrWhiteSpace(result.Sector));
        Assert.False(string.IsNullOrWhiteSpace(result.Industry));
        Assert.False(string.IsNullOrWhiteSpace(result.Website));
        Assert.False(string.IsNullOrWhiteSpace(result.LongBusinessSummary));
        Assert.True(result.FullTimeEmployees is > 0);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_GetCompanyProfileAsync_Aapl_ReturnsFundamentalsAndDetailsFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetCompanyProfileAsync("AAPL");

        Assert.Equal("AAPL", result.Symbol);
        Assert.True(result.MarketCap is > 0);
        Assert.False(string.IsNullOrWhiteSpace(result.Sector));
        Assert.False(string.IsNullOrWhiteSpace(result.Industry));
        Assert.False(string.IsNullOrWhiteSpace(result.Website));
        Assert.False(string.IsNullOrWhiteSpace(result.LongBusinessSummary));
        Assert.True(result.FullTimeEmployees is > 0);
    }
}