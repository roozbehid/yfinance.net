namespace YFinance.Net.Tests;

public sealed class YahooFinanceOptionsLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetOptionExpirationDatesAsync_Aapl_ReturnsAvailableExpirations()
    {
        using var ticker = new Ticker("AAPL");

        var expirations = await ticker.GetOptionExpirationDatesAsync();

        Assert.NotEmpty(expirations);
        Assert.All(expirations, expiration => Assert.True(expiration >= DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(-1)));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetOptionChainAsync_Aapl_ReturnsCallsPutsAndUnderlying()
    {
        using var ticker = new Ticker("AAPL");

        var result = await ticker.GetOptionChainAsync();

        Assert.Equal("AAPL", result.Symbol);
        Assert.NotEmpty(result.ExpirationDates);
        Assert.True(result.Calls.Length > 0 || result.Puts.Length > 0);
        Assert.NotNull(result.Underlying);
        Assert.Equal("AAPL", result.Underlying!.Value.Symbol);
        Assert.True(result.Underlying.Value.RegularMarketPrice is > 0);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetOptionChainAsync_AaplExplicitExpiration_RoundTripsRequestedDate()
    {
        using var ticker = new Ticker("AAPL");

        var expirations = await ticker.GetOptionExpirationDatesAsync();
        var requestedExpiration = expirations[0];

        var result = await ticker.GetOptionChainAsync(requestedExpiration);

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal(requestedExpiration, result.ExpirationDate);
        Assert.True(result.Calls.Length > 0 || result.Puts.Length > 0);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_OptionCacheModeOverloads_Aapl_ReturnConsistentData()
    {
        using var client = new YahooFinanceClient(
            options: new YahooFinanceClientOptions
            {
                Cache = new YahooFinanceCacheOptions
                {
                    Store = new MemoryYFinanceCacheStore(),
                    EnableOptionExpirationCache = true,
                    OptionExpirationCacheTtl = TimeSpan.FromMinutes(30)
                }
            });

        var refreshedExpirations = await client.GetOptionExpirationDatesAsync("AAPL", YFinanceCacheMode.Refresh);
        var cachedExpirations = await client.GetOptionExpirationDatesAsync("AAPL", YFinanceCacheMode.UseCache);

        Assert.NotEmpty(refreshedExpirations);
        Assert.Equal(refreshedExpirations, cachedExpirations);

        var requestedExpiration = refreshedExpirations[0];
        var refreshedChain = await client.GetOptionChainAsync("AAPL", requestedExpiration, YFinanceCacheMode.Refresh);
        var cachedChain = await client.GetOptionChainAsync("AAPL", requestedExpiration, YFinanceCacheMode.UseCache);

        Assert.Equal("AAPL", refreshedChain.Symbol);
        Assert.Equal("AAPL", cachedChain.Symbol);
        Assert.Equal(requestedExpiration, refreshedChain.ExpirationDate);
        Assert.Equal(requestedExpiration, cachedChain.ExpirationDate);
        Assert.NotEmpty(refreshedChain.ExpirationDates);
        Assert.NotEmpty(cachedChain.ExpirationDates);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task Ticker_OptionCacheModeOverloads_Aapl_ReturnConsistentData()
    {
        using var client = new YahooFinanceClient(
            options: new YahooFinanceClientOptions
            {
                Cache = new YahooFinanceCacheOptions
                {
                    Store = new MemoryYFinanceCacheStore(),
                    EnableOptionExpirationCache = true,
                    OptionExpirationCacheTtl = TimeSpan.FromMinutes(30)
                }
            });
        using var ticker = new Ticker("AAPL", client);

        var refreshedExpirations = await ticker.GetOptionExpirationDatesAsync(YFinanceCacheMode.Refresh);
        var cachedExpirations = await ticker.GetOptionExpirationDatesAsync(YFinanceCacheMode.UseCache);

        Assert.NotEmpty(refreshedExpirations);
        Assert.Equal(refreshedExpirations, cachedExpirations);

        var requestedExpiration = refreshedExpirations[0];
        var refreshedChain = await ticker.GetOptionChainAsync(requestedExpiration, YFinanceCacheMode.Refresh);
        var cachedChain = await ticker.GetOptionChainAsync(requestedExpiration, YFinanceCacheMode.UseCache);

        Assert.Equal("AAPL", refreshedChain.Symbol);
        Assert.Equal("AAPL", cachedChain.Symbol);
        Assert.Equal(requestedExpiration, refreshedChain.ExpirationDate);
        Assert.Equal(requestedExpiration, cachedChain.ExpirationDate);
        Assert.NotEmpty(refreshedChain.ExpirationDates);
        Assert.NotEmpty(cachedChain.ExpirationDates);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_DefaultOptionMethods_Aapl_ReturnExpirationDatesAndChain()
    {
        using var client = new YahooFinanceClient();

        var expirations = await client.GetOptionExpirationDatesAsync("AAPL");
        var requestedExpiration = expirations[0];
        var result = await client.GetOptionChainAsync("AAPL", requestedExpiration);

        Assert.NotEmpty(expirations);
        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal(requestedExpiration, result.ExpirationDate);
        Assert.True(result.Calls.Length > 0 || result.Puts.Length > 0);
    }
}