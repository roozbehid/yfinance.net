namespace YFinance.Net.Tests;

public sealed class YahooFinancePriceHistoryLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetHistoryAsync_AaplFiveDayDaily_ReturnsBarsFromYahoo()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetHistoryAsync(range: "5d", interval: "1d");

        Assert.Equal("AAPL", result.Symbol);
        Assert.False(string.IsNullOrWhiteSpace(result.Currency));
        Assert.False(string.IsNullOrWhiteSpace(result.ExchangeTimeZone));
        Assert.NotEmpty(result.Bars);
        Assert.Contains(result.Bars, bar => bar.Close is > 0);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetHistoryAsync_AaplOneYearDaily_ReturnsDividendEventsFromYahoo()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetHistoryAsync(range: "1y", interval: "1d");

        Assert.NotEmpty(result.Bars);
        Assert.NotEmpty(result.Dividends);
        Assert.Contains(result.Dividends, dividend => dividend.Amount > 0);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetHistoryAsync_IbmOneYearDaily_AdjustAllBarsDifferFromRawBars()
    {
        using var ticker = new Ticker("IBM");

        var raw = await ticker.GetHistoryAsync(range: "1y", interval: "1d", adjustmentMode: PriceAdjustmentMode.None);
        var adjusted = await ticker.GetHistoryAsync(range: "1y", interval: "1d", adjustmentMode: PriceAdjustmentMode.AdjustAll);

        Assert.Equal(raw.Bars.Length, adjusted.Bars.Length);
        Assert.NotEmpty(raw.Dividends);
        Assert.Contains(raw.Bars.Zip(adjusted.Bars), pair =>
            pair.First.Close is not null &&
            pair.Second.Close is not null &&
            pair.First.Close != pair.Second.Close);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetHistoryAsync_AaplFiveDayDaily_ExchangeLocalTimestampsDifferFromUtcOffsets()
    {
        using var ticker = new Ticker("AAPL");

        var utc = await ticker.GetHistoryAsync(range: "5d", interval: "1d", timestampMode: PriceTimestampMode.Utc);
        var local = await ticker.GetHistoryAsync(range: "5d", interval: "1d", timestampMode: PriceTimestampMode.ExchangeLocal);

        Assert.Equal(utc.Bars.Length, local.Bars.Length);
        Assert.NotEmpty(utc.Bars);
        Assert.Equal(TimeSpan.Zero, utc.Bars[0].Timestamp.Offset);
        Assert.NotEqual(TimeSpan.Zero, local.Bars[0].Timestamp.Offset);
        Assert.Equal(utc.Bars[0].Timestamp.UtcDateTime, local.Bars[0].Timestamp.UtcDateTime);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_GetPriceHistoryAsync_AaplFiveDayDaily_ReturnsBarsFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "AAPL",
            Range = "5d",
            Interval = "1d"
        });

        Assert.Equal("AAPL", result.Symbol);
        Assert.False(string.IsNullOrWhiteSpace(result.Currency));
        Assert.False(string.IsNullOrWhiteSpace(result.ExchangeTimeZone));
        Assert.NotEmpty(result.Bars);
        Assert.Contains(result.Bars, bar => bar.Close is > 0);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_GetPriceHistoryAsync_IbmAdjustAll_DiffersFromRawBars()
    {
        using var client = new YahooFinanceClient();

        var raw = await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "IBM",
            Range = "1y",
            Interval = "1d",
            AdjustmentMode = PriceAdjustmentMode.None
        });
        var adjusted = await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "IBM",
            Range = "1y",
            Interval = "1d",
            AdjustmentMode = PriceAdjustmentMode.AdjustAll
        });

        Assert.Equal(raw.Bars.Length, adjusted.Bars.Length);
        Assert.NotEmpty(raw.Dividends);
        Assert.Contains(raw.Bars.Zip(adjusted.Bars), pair =>
            pair.First.Close is not null &&
            pair.Second.Close is not null &&
            pair.First.Close != pair.Second.Close);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_GetPriceHistoryAsync_AaplExchangeLocalTimestampsDifferFromUtcOffsets()
    {
        using var client = new YahooFinanceClient();

        var utc = await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "AAPL",
            Range = "5d",
            Interval = "1d",
            TimestampMode = PriceTimestampMode.Utc
        });
        var local = await client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = "AAPL",
            Range = "5d",
            Interval = "1d",
            TimestampMode = PriceTimestampMode.ExchangeLocal
        });

        Assert.Equal(utc.Bars.Length, local.Bars.Length);
        Assert.NotEmpty(utc.Bars);
        Assert.Equal(TimeSpan.Zero, utc.Bars[0].Timestamp.Offset);
        Assert.NotEqual(TimeSpan.Zero, local.Bars[0].Timestamp.Offset);
        Assert.Equal(utc.Bars[0].Timestamp.UtcDateTime, local.Bars[0].Timestamp.UtcDateTime);
    }
}