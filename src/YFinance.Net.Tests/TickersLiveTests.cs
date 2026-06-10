namespace YFinance.Net.Tests;

public sealed class TickersLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetHistoriesAsync_AaplAndMsft_ReturnsMultiTickerHistoryThroughFacade()
    {
        using var tickers = new Tickers(new[] { "AAPL", "MSFT" });

        var result = await tickers.GetHistoriesAsync(range: "5d", interval: "1d", maxConcurrency: 2);

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.Histories.Count);
        Assert.NotEmpty(result.Histories["AAPL"].Bars);
        Assert.NotEmpty(result.Histories["MSFT"].Bars);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetCompanyProfilesAsync_AaplAndMsft_ReturnsMultiTickerProfilesThroughFacade()
    {
        using var tickers = new Tickers(new[] { "AAPL", "MSFT" });

        var result = await tickers.GetCompanyProfilesAsync(maxConcurrency: 2);

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.Profiles.Count);
        Assert.True(result.Profiles.ContainsKey("AAPL"));
        Assert.True(result.Profiles.ContainsKey("MSFT"));
        Assert.True(result.Profiles["AAPL"].MarketCap is > 0);
        Assert.True(result.Profiles["MSFT"].MarketCap is > 0);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetNewsAsync_AaplAndMsft_ReturnsMultiTickerNewsThroughFacade()
    {
        using var tickers = new Tickers(new[] { "AAPL", "MSFT" });

        var result = await tickers.GetNewsAsync(count: 1, maxConcurrency: 2);

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.NewsBySymbol.Count);
        Assert.True(result.NewsBySymbol.ContainsKey("AAPL"));
        Assert.True(result.NewsBySymbol.ContainsKey("MSFT"));
        Assert.NotEmpty(result.NewsBySymbol["AAPL"]);
        Assert.NotEmpty(result.NewsBySymbol["MSFT"]);
        Assert.Contains(result.NewsBySymbol["AAPL"], item => !string.IsNullOrWhiteSpace(item.Title));
        Assert.Contains(result.NewsBySymbol["MSFT"], item => !string.IsNullOrWhiteSpace(item.Title));
    }
}