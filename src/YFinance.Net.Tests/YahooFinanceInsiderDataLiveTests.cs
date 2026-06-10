namespace YFinance.Net.Tests;

public sealed class YahooFinanceInsiderDataLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetInsiderDataAsync_Googl_ReturnsTransactionsAndPurchaseActivity()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetInsiderDataAsync("GOOGL");

        Assert.NotEmpty(result.Transactions);
        Assert.NotNull(result.PurchaseActivity);
        Assert.All(result.Transactions, transaction => Assert.False(string.IsNullOrWhiteSpace(transaction.Insider)));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task TickerFacade_GetInsiderDataAsync_Googl_ReturnsRosterHolders()
    {
        using var ticker = new Ticker("GOOGL");

        var result = await ticker.GetInsiderDataAsync();

        Assert.NotEmpty(result.RosterHolders);
        Assert.All(result.RosterHolders, holder => Assert.False(string.IsNullOrWhiteSpace(holder.Name)));
    }
}