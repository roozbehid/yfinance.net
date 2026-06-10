namespace YFinance.Net.Tests;

public sealed class YahooFinanceScreenerLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetPredefinedScreenerAsync_DayGainers_ReturnsRowsFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetPredefinedScreenerAsync(PredefinedScreenerId.DayGainers, new PredefinedScreenerOptions
        {
            Count = 5
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Title));
        Assert.NotEmpty(result.Quotes);
        Assert.True(result.Quotes.Length <= 5);
        Assert.Contains(result.Quotes, quote => !string.IsNullOrWhiteSpace(quote.Symbol));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task ScreenAsync_CustomEquityQuery_ReturnsRowsFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.ScreenAsync(
            ScreenerQuery.And(
                ScreenerQuery.GreaterThan(ScreenerFields.Trading.PercentChange, 3),
                ScreenerQuery.Equal(ScreenerFields.Common.Region, "us")),
            new ScreenerOptions
            {
                Count = 5,
                QuoteType = ScreenerQuoteType.Equity
            }.WithSort(ScreenerFields.Common.Symbol));

        Assert.NotEmpty(result.Quotes);
        Assert.True(result.Quotes.Length <= 5);
        Assert.Contains(result.Quotes, quote => !string.IsNullOrWhiteSpace(quote.Symbol));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task ScreenAsync_DayGainersPreset_ReturnsRowsFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.ScreenAsync(ScreenerPresets.DayGainers().WithCount(5));

        Assert.NotEmpty(result.Quotes);
        Assert.True(result.Quotes.Length <= 5);
        Assert.Contains(result.Quotes, quote => !string.IsNullOrWhiteSpace(quote.Symbol));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task ScreenAsync_TopEtfsUsPreset_ReturnsRowsFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.ScreenAsync(ScreenerPresets.TopEtfsUs().WithCount(5));

        Assert.NotEmpty(result.Quotes);
        Assert.True(result.Quotes.Length <= 5);
        Assert.Contains(result.Quotes, quote => string.Equals(quote.QuoteType, "ETF", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task ScreenAsync_HighYieldBondPreset_ReturnsRowsFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.ScreenAsync(ScreenerPresets.HighYieldBond().WithCount(5));

        Assert.NotEmpty(result.Quotes);
        Assert.True(result.Quotes.Length <= 5);
        Assert.Contains(result.Quotes, quote => string.Equals(quote.QuoteType, "MUTUALFUND", StringComparison.OrdinalIgnoreCase));
    }
}