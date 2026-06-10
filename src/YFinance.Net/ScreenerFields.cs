namespace YFinance.Net;

public readonly record struct ScreenerField
{
    public ScreenerField(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    public string Name { get; }

    public override string ToString() => Name;
}

public static class ScreenerFields
{
    public static class Common
    {
        public static ScreenerField Symbol { get; } = new("ticker");

        public static ScreenerField Region { get; } = new("region");

        public static ScreenerField Exchange { get; } = new("exchange");

        public static ScreenerField Sector { get; } = new("sector");

        public static ScreenerField Industry { get; } = new("industry");
    }

    public static class Trading
    {
        public static ScreenerField PercentChange { get; } = new("percentchange");

        public static ScreenerField CurrentPrice { get; } = new("intradayprice");

        public static ScreenerField MarketCap { get; } = new("intradaymarketcap");

        public static ScreenerField DayVolume { get; } = new("dayvolume");

        public static ScreenerField AverageDailyVolumeThreeMonths { get; } = new("avgdailyvol3m");

        public static ScreenerField ShortPercentageOfSharesOutstanding { get; } = new("short_percentage_of_shares_outstanding.value");
    }

    public static class Valuation
    {
        public static ScreenerField TrailingPriceEarnings { get; } = new("peratio.lasttwelvemonths");

        public static ScreenerField PegRatioFiveYears { get; } = new("pegratio_5y");
    }

    public static class Growth
    {
        public static ScreenerField EarningsGrowthTrailingTwelveMonths { get; } = new("epsgrowth.lasttwelvemonths");

        public static ScreenerField QuarterlyRevenueGrowth { get; } = new("quarterlyrevenuegrowth.quarterly");
    }

    public static class Funds
    {
        public static ScreenerField CategoryName { get; } = new("categoryname");

        public static ScreenerField NetAssets { get; } = new("fundnetassets");

        public static ScreenerField ExpenseRatio { get; } = new("annualreportnetexpenseratio");

        public static ScreenerField PerformanceRatingOverall { get; } = new("performanceratingoverall");

        public static ScreenerField RiskRatingOverall { get; } = new("riskratingoverall");

        public static ScreenerField InitialInvestment { get; } = new("initialinvestment");

        public static ScreenerField OneYearCategoryRank { get; } = new("annualreturnnavy1categoryrank");
    }
}