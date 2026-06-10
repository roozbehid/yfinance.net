namespace YFinance.Net.Tests;

public sealed class YahooFinanceCashFlowLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetCashFlowAsync_AaplAnnual_ReturnsPeriodsAndRows()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetCashFlowAsync("AAPL");

        Assert.NotEmpty(result.Periods);
        Assert.NotEmpty(result.LineItems);
        Assert.Contains(result.LineItems, item => item.Key == "OperatingCashFlow");
        Assert.Contains(result.LineItems, item => item.Key == "CapitalExpenditure");
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task TickerFacade_GetCashFlowAsync_AaplQuarterly_ReturnsPeriodsAndRows()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetCashFlowAsync(FinancialStatementFrequency.Quarterly);

        Assert.NotEmpty(result.Periods);
        Assert.NotEmpty(result.LineItems);
        Assert.Contains(result.LineItems, item => item.Key == "OperatingCashFlow" || item.Key == "FreeCashFlow");
    }
}