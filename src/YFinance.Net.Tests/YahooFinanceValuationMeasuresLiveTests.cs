namespace YFinance.Net.Tests;

public sealed class YahooFinanceValuationMeasuresLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetValuationMeasuresAsync_Aapl_ReturnsRowsAndColumns()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetValuationMeasuresAsync("AAPL");

        Assert.False(result.IsEmpty);
        Assert.NotEmpty(result.Columns);
        Assert.NotEmpty(result.Rows);
        Assert.Contains(result.Rows, row => !string.IsNullOrWhiteSpace(row.Metric));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task TickerFacade_GetValuationMeasuresAsync_Aapl_ReturnsRowsAndColumns()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetValuationMeasuresAsync();

        Assert.False(result.IsEmpty);
        Assert.NotEmpty(result.Columns);
        Assert.NotEmpty(result.Rows);
    }
}