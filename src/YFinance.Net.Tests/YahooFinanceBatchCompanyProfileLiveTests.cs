namespace YFinance.Net.Tests;

public sealed class YahooFinanceBatchCompanyProfileLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task GetCompanyProfilesAsync_AaplAndMsft_ReturnsBothWithoutFailures()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetCompanyProfilesAsync(new BatchCompanyProfileRequest(new[] { "AAPL", "MSFT" }, 2));

        Assert.Empty(result.Failures);
        Assert.Equal(2, result.Profiles.Count);
        Assert.True(result.Profiles.ContainsKey("AAPL"));
        Assert.True(result.Profiles.ContainsKey("MSFT"));
        Assert.True(result.Profiles["AAPL"].MarketCap is > 0);
        Assert.True(result.Profiles["MSFT"].MarketCap is > 0);
    }
}