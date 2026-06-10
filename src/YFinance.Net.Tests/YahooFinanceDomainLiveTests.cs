namespace YFinance.Net.Tests;

public sealed class YahooFinanceDomainLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task Sector_UsAndGbRegions_ReturnDifferentTopCompanyShapes()
    {
        using var usSector = new Sector("technology", "US");
        using var gbSector = new Sector("technology", "GB");

        var us = await usSector.GetAsync();
        var gb = await gbSector.GetAsync();

        Assert.NotEmpty(us.TopCompanies);
        Assert.NotEmpty(gb.TopCompanies);
        Assert.Contains(gb.TopCompanies, company => company.Symbol.EndsWith(".L", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(us.TopCompanies, company => company.Symbol.EndsWith(".L", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task Industry_GetAsync_ReturnsSectorLinkAndCompanyLists()
    {
        using var industry = new Industry("software-infrastructure", "US");

        var result = await industry.GetAsync();

        Assert.Equal("technology", result.SectorKey);
        Assert.NotEmpty(result.TopCompanies);
        Assert.NotEmpty(result.TopPerformingCompanies);
        Assert.NotEmpty(result.TopGrowthCompanies);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_GetSectorAsync_UsAndGbRegions_ReturnDifferentTopCompanyShapes()
    {
        using var client = new YahooFinanceClient();

        var us = await client.GetSectorAsync("technology", "US");
        var gb = await client.GetSectorAsync("technology", "GB");

        Assert.NotEmpty(us.TopCompanies);
        Assert.NotEmpty(gb.TopCompanies);
        Assert.Contains(gb.TopCompanies, company => company.Symbol.EndsWith(".L", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(us.TopCompanies, company => company.Symbol.EndsWith(".L", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_GetIndustryAsync_ReturnsSectorLinkAndCompanyLists()
    {
        using var client = new YahooFinanceClient();

        var result = await client.GetIndustryAsync("software-infrastructure", "US");

        Assert.Equal("technology", result.SectorKey);
        Assert.NotEmpty(result.TopCompanies);
        Assert.NotEmpty(result.TopPerformingCompanies);
        Assert.NotEmpty(result.TopGrowthCompanies);
    }
}