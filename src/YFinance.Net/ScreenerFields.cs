namespace YFinance.Net;

/// <summary>
/// Represents a field name used in a Yahoo Finance screener query.
/// </summary>
public readonly record struct ScreenerField
{
    /// <summary>
    /// Initializes a screener field wrapper.
    /// </summary>
    /// <param name="name">Yahoo Finance field name.</param>
    public ScreenerField(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Gets the Yahoo Finance field name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Returns the Yahoo Finance field name.
    /// </summary>
    public override string ToString() => Name;
}

/// <summary>
/// Commonly used Yahoo Finance screener fields grouped by category.
/// </summary>
public static class ScreenerFields
{
    /// <summary>
    /// Common identity and classification fields.
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// Ticker symbol field.
        /// </summary>
        public static ScreenerField Symbol { get; } = new("ticker");

        /// <summary>
        /// Region field.
        /// </summary>
        public static ScreenerField Region { get; } = new("region");

        /// <summary>
        /// Exchange field.
        /// </summary>
        public static ScreenerField Exchange { get; } = new("exchange");

        /// <summary>
        /// Sector field.
        /// </summary>
        public static ScreenerField Sector { get; } = new("sector");

        /// <summary>
        /// Industry field.
        /// </summary>
        public static ScreenerField Industry { get; } = new("industry");
    }

    /// <summary>
    /// Trading and liquidity related fields.
    /// </summary>
    public static class Trading
    {
        /// <summary>
        /// Percentage price change field.
        /// </summary>
        public static ScreenerField PercentChange { get; } = new("percentchange");

        /// <summary>
        /// Current price field.
        /// </summary>
        public static ScreenerField CurrentPrice { get; } = new("intradayprice");

        /// <summary>
        /// Market capitalization field.
        /// </summary>
        public static ScreenerField MarketCap { get; } = new("intradaymarketcap");

        /// <summary>
        /// Current trading volume field.
        /// </summary>
        public static ScreenerField DayVolume { get; } = new("dayvolume");

        /// <summary>
        /// Three month average daily volume field.
        /// </summary>
        public static ScreenerField AverageDailyVolumeThreeMonths { get; } = new("avgdailyvol3m");

        /// <summary>
        /// Short interest as a percentage of shares outstanding.
        /// </summary>
        public static ScreenerField ShortPercentageOfSharesOutstanding { get; } = new("short_percentage_of_shares_outstanding.value");
    }

    /// <summary>
    /// Valuation-related fields.
    /// </summary>
    public static class Valuation
    {
        /// <summary>
        /// Trailing price-to-earnings ratio.
        /// </summary>
        public static ScreenerField TrailingPriceEarnings { get; } = new("peratio.lasttwelvemonths");

        /// <summary>
        /// Five-year PEG ratio.
        /// </summary>
        public static ScreenerField PegRatioFiveYears { get; } = new("pegratio_5y");
    }

    /// <summary>
    /// Growth-related fields.
    /// </summary>
    public static class Growth
    {
        /// <summary>
        /// Trailing twelve month EPS growth field.
        /// </summary>
        public static ScreenerField EarningsGrowthTrailingTwelveMonths { get; } = new("epsgrowth.lasttwelvemonths");

        /// <summary>
        /// Quarterly revenue growth field.
        /// </summary>
        public static ScreenerField QuarterlyRevenueGrowth { get; } = new("quarterlyrevenuegrowth.quarterly");
    }

    /// <summary>
    /// Mutual fund and ETF-specific fields.
    /// </summary>
    public static class Funds
    {
        /// <summary>
        /// Fund category name field.
        /// </summary>
        public static ScreenerField CategoryName { get; } = new("categoryname");

        /// <summary>
        /// Net assets field.
        /// </summary>
        public static ScreenerField NetAssets { get; } = new("fundnetassets");

        /// <summary>
        /// Expense ratio field.
        /// </summary>
        public static ScreenerField ExpenseRatio { get; } = new("annualreportnetexpenseratio");

        /// <summary>
        /// Overall performance rating field.
        /// </summary>
        public static ScreenerField PerformanceRatingOverall { get; } = new("performanceratingoverall");

        /// <summary>
        /// Overall risk rating field.
        /// </summary>
        public static ScreenerField RiskRatingOverall { get; } = new("riskratingoverall");

        /// <summary>
        /// Minimum initial investment field.
        /// </summary>
        public static ScreenerField InitialInvestment { get; } = new("initialinvestment");

        /// <summary>
        /// One-year category rank field.
        /// </summary>
        public static ScreenerField OneYearCategoryRank { get; } = new("annualreturnnavy1categoryrank");
    }
}