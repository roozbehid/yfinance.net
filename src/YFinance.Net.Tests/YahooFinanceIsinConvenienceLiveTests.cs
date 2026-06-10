namespace YFinance.Net.Tests;

public sealed class YahooFinanceIsinConvenienceLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetByIsinAsync_UsAppleIsin_ReturnsAppleTicker()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetByIsinAsync("US0378331005");

        Assert.NotNull(result.Ticker);
        Assert.Equal("AAPL", result.Ticker!.Value.Symbol);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetInfoByIsinAsync_SpanishIsin_ReturnsFluidraTicker()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetInfoByIsinAsync("ES0137650018");

        Assert.NotNull(result);
        Assert.Equal("FDR.MC", result!.Value.Symbol);
    }
}