namespace YFinance.Net.Tests;

public sealed class YahooFinanceHoldersLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetHoldersAsync_Googl_ReturnsMajorAndInstitutionalHolders()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetHoldersAsync("GOOGL");

        Assert.NotNull(result.MajorHolders);
        Assert.True(result.MajorHolders!.InstitutionsCount > 0);
        Assert.NotEmpty(result.InstitutionalHolders);
        Assert.All(result.InstitutionalHolders, holder => Assert.False(string.IsNullOrWhiteSpace(holder.Holder)));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task TickerFacade_GetHoldersAsync_Googl_ReturnsMajorAndMutualFundHolders()
    {
        using var ticker = new Ticker("GOOGL");

        var result = await ticker.GetHoldersAsync();

        Assert.NotNull(result.MajorHolders);
        Assert.NotEmpty(result.MutualFundHolders);
        Assert.All(result.MutualFundHolders, holder => Assert.False(string.IsNullOrWhiteSpace(holder.Holder)));
    }
}