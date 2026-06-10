namespace YFinance.Net.Tests;

public sealed class YahooFinanceBatchPriceHistoryLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetPriceHistoriesAsync_AaplAndMsft_ReturnsBothWithoutFailures()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetPriceHistoriesAsync(new BatchPriceHistoryRequest
        {
            Symbols = new[] { "AAPL", "MSFT" },
            Range = "5d",
            Interval = "1d",
            MaxConcurrency = 2
        });

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.Histories.Count);
        Assert.True(result.Histories.ContainsKey("AAPL"));
        Assert.True(result.Histories.ContainsKey("MSFT"));
        Assert.NotEmpty(result.Histories["AAPL"].Bars);
        Assert.NotEmpty(result.Histories["MSFT"].Bars);
    }
}