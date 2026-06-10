namespace YFinance.Net;

public static class ScreenerPresets
{
    public static ScreenerDefinition DayGainers(
        string region = "us",
        decimal minimumPercentChange = 3m,
        decimal minimumPrice = 5m,
        decimal minimumMarketCap = 2_000_000_000m,
        long minimumDayVolume = 15_000)
    {
        var normalizedRegion = NormalizeRegionFilter(region);

        return new ScreenerDefinition
        {
            Query = ScreenerQuery.And(
                ScreenerQuery.GreaterThan(ScreenerFields.Trading.PercentChange, minimumPercentChange),
                ScreenerQuery.Equal(ScreenerFields.Common.Region, normalizedRegion),
                ScreenerQuery.GreaterThanOrEqual(ScreenerFields.Trading.MarketCap, minimumMarketCap),
                ScreenerQuery.GreaterThanOrEqual(ScreenerFields.Trading.CurrentPrice, minimumPrice),
                ScreenerQuery.GreaterThan(ScreenerFields.Trading.DayVolume, minimumDayVolume)),
            Options = CreateEquityOptions(region).WithSort(ScreenerFields.Trading.PercentChange)
        };
    }

    public static ScreenerDefinition DayLosers(
        string region = "us",
        decimal maximumPercentChange = -2.5m,
        decimal minimumPrice = 5m,
        decimal minimumMarketCap = 2_000_000_000m,
        long minimumDayVolume = 20_000)
    {
        var normalizedRegion = NormalizeRegionFilter(region);

        return new ScreenerDefinition
        {
            Query = ScreenerQuery.And(
                ScreenerQuery.LessThan(ScreenerFields.Trading.PercentChange, maximumPercentChange),
                ScreenerQuery.Equal(ScreenerFields.Common.Region, normalizedRegion),
                ScreenerQuery.GreaterThanOrEqual(ScreenerFields.Trading.MarketCap, minimumMarketCap),
                ScreenerQuery.GreaterThanOrEqual(ScreenerFields.Trading.CurrentPrice, minimumPrice),
                ScreenerQuery.GreaterThan(ScreenerFields.Trading.DayVolume, minimumDayVolume)),
            Options = CreateEquityOptions(region).WithSort(ScreenerFields.Trading.PercentChange, ScreenerSortOrder.Ascending)
        };
    }

    public static ScreenerDefinition MostActives(
        string region = "us",
        decimal minimumMarketCap = 2_000_000_000m,
        long minimumDayVolume = 5_000_000)
    {
        var normalizedRegion = NormalizeRegionFilter(region);

        return new ScreenerDefinition
        {
            Query = ScreenerQuery.And(
                ScreenerQuery.Equal(ScreenerFields.Common.Region, normalizedRegion),
                ScreenerQuery.GreaterThanOrEqual(ScreenerFields.Trading.MarketCap, minimumMarketCap),
                ScreenerQuery.GreaterThan(ScreenerFields.Trading.DayVolume, minimumDayVolume)),
            Options = CreateEquityOptions(region).WithSort(ScreenerFields.Trading.DayVolume)
        };
    }

    public static ScreenerDefinition HighYieldBond(
        decimal maximumInitialInvestment = 100_001m,
        int maximumOneYearCategoryRank = 50)
    {
        return new ScreenerDefinition
        {
            Query = ScreenerQuery.And(
                PerformanceRatingAtLeastFour(),
                ScreenerQuery.LessThan(ScreenerFields.Funds.InitialInvestment, maximumInitialInvestment),
                ScreenerQuery.LessThan(ScreenerFields.Funds.OneYearCategoryRank, maximumOneYearCategoryRank),
                ConservativeRiskRating(),
                ScreenerQuery.Equal(ScreenerFields.Funds.CategoryName, "High Yield Bond"),
                NasdaqFundExchange()),
            Options = CreateMutualFundOptions().WithSort(ScreenerFields.Funds.NetAssets)
        };
    }

    public static ScreenerDefinition TopMutualFunds(
        decimal minimumPrice = 15m,
        decimal minimumInitialInvestment = 1_000m)
    {
        return new ScreenerDefinition
        {
            Query = ScreenerQuery.And(
                ScreenerQuery.GreaterThan(ScreenerFields.Trading.CurrentPrice, minimumPrice),
                PerformanceRatingAtLeastFour(),
                ScreenerQuery.GreaterThan(ScreenerFields.Funds.InitialInvestment, minimumInitialInvestment),
                NasdaqFundExchange()),
            Options = CreateMutualFundOptions().WithSort(ScreenerFields.Trading.PercentChange)
        };
    }

    public static ScreenerDefinition TopEtfsUs(
        string region = "us",
        decimal minimumPrice = 10m)
    {
        var normalizedRegion = NormalizeRegionFilter(region);

        return new ScreenerDefinition
        {
            Query = ScreenerQuery.And(
                ScreenerQuery.GreaterThan(ScreenerFields.Trading.CurrentPrice, minimumPrice),
                PerformanceRatingAtLeastFour(),
                ScreenerQuery.Equal(ScreenerFields.Common.Region, normalizedRegion)),
            Options = CreateEtfOptions(region).WithSort(ScreenerFields.Trading.PercentChange)
        };
    }

    public static ScreenerDefinition TopPerformingEtfs(
        string region = "us",
        decimal minimumPrice = 10m)
    {
        var normalizedRegion = NormalizeRegionFilter(region);

        return new ScreenerDefinition
        {
            Query = ScreenerQuery.And(
                ScreenerQuery.Equal(ScreenerFields.Common.Region, normalizedRegion),
                PerformanceRatingAtLeastFour(),
                ScreenerQuery.GreaterThan(ScreenerFields.Trading.CurrentPrice, minimumPrice)),
            Options = CreateEtfOptions(region).WithSort(ScreenerFields.Funds.ExpenseRatio, ScreenerSortOrder.Ascending)
        };
    }

    public static ScreenerDefinition TechnologyEtfs(string region = "us")
    {
        var normalizedRegion = NormalizeRegionFilter(region);

        return new ScreenerDefinition
        {
            Query = ScreenerQuery.And(
                ScreenerQuery.Equal(ScreenerFields.Common.Region, normalizedRegion),
                ScreenerQuery.Equal(ScreenerFields.Funds.CategoryName, "Technology")),
            Options = CreateEtfOptions(region).WithSort(ScreenerFields.Funds.ExpenseRatio, ScreenerSortOrder.Ascending)
        };
    }

    private static ScreenerOptions CreateEquityOptions(string region)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);

        return new ScreenerOptions
        {
            QuoteType = ScreenerQuoteType.Equity,
            Region = region.Trim().ToUpperInvariant()
        };
    }

    private static ScreenerOptions CreateMutualFundOptions(string region = "US")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);

        return new ScreenerOptions
        {
            QuoteType = ScreenerQuoteType.MutualFund,
            Region = region.Trim().ToUpperInvariant()
        };
    }

    private static ScreenerOptions CreateEtfOptions(string region)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);

        return new ScreenerOptions
        {
            QuoteType = ScreenerQuoteType.Etf,
            Region = region.Trim().ToUpperInvariant()
        };
    }

    private static ScreenerQuery PerformanceRatingAtLeastFour()
    {
        return ScreenerQuery.AnyOf(ScreenerFields.Funds.PerformanceRatingOverall, 4, 5);
    }

    private static ScreenerQuery ConservativeRiskRating()
    {
        return ScreenerQuery.AnyOf(ScreenerFields.Funds.RiskRatingOverall, 1, 2, 3);
    }

    private static ScreenerQuery NasdaqFundExchange()
    {
        return ScreenerQuery.Equal(ScreenerFields.Common.Exchange, "NAS");
    }

    private static string NormalizeRegionFilter(string region)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        return region.Trim().ToLowerInvariant();
    }
}