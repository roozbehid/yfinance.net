namespace YFinance.Net.Tests;

public sealed class YahooFinanceIncomeStatementLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetIncomeStatementAsync_AaplAnnual_ReturnsPeriodsAndRows()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetIncomeStatementAsync("AAPL");

        Assert.NotEmpty(result.Periods);
        Assert.NotEmpty(result.LineItems);
        Assert.Contains(result.LineItems, item => item.Key == "TotalRevenue");
        Assert.Contains(result.LineItems, item => item.Key == "BasicEPS");
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task TickerFacade_GetIncomeStatementAsync_AaplQuarterly_ReturnsPeriodsAndRows()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetIncomeStatementAsync(FinancialStatementFrequency.Quarterly);

        Assert.NotEmpty(result.Periods);
        Assert.NotEmpty(result.LineItems);
        Assert.Contains(result.LineItems, item => item.Key == "TotalRevenue");
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task TickerFacade_GetTrailingIncomeStatementAsync_Aapl_ReturnsTtmRows()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetTrailingIncomeStatementAsync();

        Assert.Equal(FinancialStatementFrequency.Trailing, result.Frequency);
        Assert.NotEmpty(result.Periods);
        Assert.Contains(result.LineItems, item => item.Key == "TotalRevenue");
        Assert.Contains(result.LineItems, item => item.Key == "PretaxIncome");
        Assert.All(result.Periods, period => Assert.Equal("TTM", period.PeriodType));
    }
}