namespace YFinance.Net.Tests;

public sealed class YahooFinanceBalanceSheetLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetBalanceSheetAsync_AaplAnnual_ReturnsPeriodsAndRows()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetBalanceSheetAsync("AAPL");

        Assert.NotEmpty(result.Periods);
        Assert.NotEmpty(result.LineItems);
        Assert.Contains(result.LineItems, item => item.Key == "TotalAssets");
        Assert.Contains(result.LineItems, item => item.Key == "NetPPE");
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task TickerFacade_GetBalanceSheetAsync_AaplQuarterly_ReturnsPeriodsAndRows()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetBalanceSheetAsync(FinancialStatementFrequency.Quarterly);

        Assert.NotEmpty(result.Periods);
        Assert.NotEmpty(result.LineItems);
        Assert.Contains(result.LineItems, item => item.Key == "TotalAssets" || item.Key == "CurrentAssets");
    }
}