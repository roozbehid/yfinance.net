namespace YFinance.Net.Tests;

public sealed class YahooFinanceLookupLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task LookupAsync_AaplEquity_ReturnsMatchingDocumentFromYahoo()
    {
        using var client = new YahooFinanceClient();

        var result = await client.LookupAsync(new LookupRequest
        {
            Query = "AAPL",
            Type = LookupType.Equity,
            Count = 5
        });

        Assert.NotEmpty(result.Documents);
        Assert.True(result.Documents.Length <= 5);
        Assert.Contains(result.Documents, document => string.Equals(document.Symbol, "AAPL", StringComparison.OrdinalIgnoreCase));
    }
}