namespace YFinance.Net;

/// <summary>
/// Analyst insights returned by Yahoo Finance for a symbol.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="PriceTargets">Analyst price targets.</param>
/// <param name="Recommendations">Recommendation trend history.</param>
/// <param name="UpgradesDowngrades">Upgrade and downgrade history.</param>
/// <param name="EarningsHistory">Earnings history.</param>
/// <param name="EarningsEstimates">Periodic earnings estimates.</param>
/// <param name="RevenueEstimates">Periodic revenue estimates.</param>
/// <param name="EpsTrends">EPS trend history.</param>
/// <param name="EpsRevisions">EPS revisions.</param>
/// <param name="GrowthEstimates">Growth estimates.</param>
public sealed record AnalystInsights(
    string Symbol,
    AnalystPriceTargets PriceTargets,
    IReadOnlyList<RecommendationTrendEntry> Recommendations,
    IReadOnlyList<UpgradeDowngradeEntry> UpgradesDowngrades,
    IReadOnlyList<EarningsHistoryEntry> EarningsHistory,
    IReadOnlyList<PeriodicEarningsEstimate> EarningsEstimates,
    IReadOnlyList<PeriodicRevenueEstimate> RevenueEstimates,
    IReadOnlyList<PeriodicEpsTrend> EpsTrends,
    IReadOnlyList<PeriodicEpsRevisions> EpsRevisions,
    IReadOnlyList<GrowthEstimate> GrowthEstimates);

/// <summary>
/// Analyst price target summary.
/// </summary>
/// <param name="Current">Current price.</param>
/// <param name="Low">Lowest target price.</param>
/// <param name="High">Highest target price.</param>
/// <param name="Mean">Mean target price.</param>
/// <param name="Median">Median target price.</param>
public sealed record AnalystPriceTargets(
    decimal? Current,
    decimal? Low,
    decimal? High,
    decimal? Mean,
    decimal? Median);

/// <summary>
/// Analyst recommendation trend for a reporting period.
/// </summary>
/// <param name="Period">Reporting period label.</param>
/// <param name="StrongBuy">Strong buy count.</param>
/// <param name="Buy">Buy count.</param>
/// <param name="Hold">Hold count.</param>
/// <param name="Sell">Sell count.</param>
/// <param name="StrongSell">Strong sell count.</param>
public sealed record RecommendationTrendEntry(
    string Period,
    int? StrongBuy,
    int? Buy,
    int? Hold,
    int? Sell,
    int? StrongSell);

/// <summary>
/// Upgrade or downgrade action reported by Yahoo Finance.
/// </summary>
/// <param name="GradeDate">Date of the grade change.</param>
/// <param name="Firm">Analyst firm.</param>
/// <param name="ToGrade">New grade.</param>
/// <param name="FromGrade">Previous grade.</param>
/// <param name="Action">Action type.</param>
/// <param name="PriceTargetAction">Price target action label.</param>
/// <param name="CurrentPriceTarget">Current price target.</param>
/// <param name="PriorPriceTarget">Prior price target.</param>
public sealed record UpgradeDowngradeEntry(
    DateTimeOffset? GradeDate,
    string? Firm,
    string? ToGrade,
    string? FromGrade,
    string? Action,
    string? PriceTargetAction,
    decimal? CurrentPriceTarget,
    decimal? PriorPriceTarget);

/// <summary>
/// Historical earnings result entry.
/// </summary>
/// <param name="Period">Reporting period label.</param>
/// <param name="Quarter">Quarter timestamp.</param>
/// <param name="EpsActual">Actual EPS.</param>
/// <param name="EpsEstimate">Estimated EPS.</param>
/// <param name="EpsDifference">Difference between actual and estimate.</param>
/// <param name="SurprisePercent">Surprise percentage.</param>
/// <param name="Currency">Currency.</param>
public sealed record EarningsHistoryEntry(
    string? Period,
    DateTimeOffset? Quarter,
    decimal? EpsActual,
    decimal? EpsEstimate,
    decimal? EpsDifference,
    decimal? SurprisePercent,
    string? Currency);

/// <summary>
/// Periodic earnings estimate entry.
/// </summary>
/// <param name="Period">Reporting period label.</param>
/// <param name="EndDate">Period end date.</param>
/// <param name="NumberOfAnalysts">Number of analysts.</param>
/// <param name="Average">Average estimate.</param>
/// <param name="Low">Lowest estimate.</param>
/// <param name="High">Highest estimate.</param>
/// <param name="YearAgoEps">EPS from the year-ago period.</param>
/// <param name="Growth">Growth percentage.</param>
/// <param name="Currency">Currency.</param>
public sealed record PeriodicEarningsEstimate(
    string Period,
    DateOnly? EndDate,
    int? NumberOfAnalysts,
    decimal? Average,
    decimal? Low,
    decimal? High,
    decimal? YearAgoEps,
    decimal? Growth,
    string? Currency);

/// <summary>
/// Periodic revenue estimate entry.
/// </summary>
/// <param name="Period">Reporting period label.</param>
/// <param name="EndDate">Period end date.</param>
/// <param name="NumberOfAnalysts">Number of analysts.</param>
/// <param name="Average">Average estimate.</param>
/// <param name="Low">Lowest estimate.</param>
/// <param name="High">Highest estimate.</param>
/// <param name="YearAgoRevenue">Revenue from the year-ago period.</param>
/// <param name="Growth">Growth percentage.</param>
/// <param name="Currency">Currency.</param>
public sealed record PeriodicRevenueEstimate(
    string Period,
    DateOnly? EndDate,
    int? NumberOfAnalysts,
    decimal? Average,
    decimal? Low,
    decimal? High,
    decimal? YearAgoRevenue,
    decimal? Growth,
    string? Currency);

/// <summary>
/// EPS trend snapshot across multiple revision windows.
/// </summary>
/// <param name="Period">Reporting period label.</param>
/// <param name="EndDate">Period end date.</param>
/// <param name="Current">Current estimate.</param>
/// <param name="SevenDaysAgo">Estimate from seven days ago.</param>
/// <param name="ThirtyDaysAgo">Estimate from thirty days ago.</param>
/// <param name="SixtyDaysAgo">Estimate from sixty days ago.</param>
/// <param name="NinetyDaysAgo">Estimate from ninety days ago.</param>
/// <param name="Currency">Currency.</param>
public sealed record PeriodicEpsTrend(
    string Period,
    DateOnly? EndDate,
    decimal? Current,
    decimal? SevenDaysAgo,
    decimal? ThirtyDaysAgo,
    decimal? SixtyDaysAgo,
    decimal? NinetyDaysAgo,
    string? Currency);

/// <summary>
/// EPS revision counts across recent windows.
/// </summary>
/// <param name="Period">Reporting period label.</param>
/// <param name="EndDate">Period end date.</param>
/// <param name="UpLast7Days">Up revisions in the last seven days.</param>
/// <param name="UpLast30Days">Up revisions in the last thirty days.</param>
/// <param name="DownLast7Days">Down revisions in the last seven days.</param>
/// <param name="DownLast30Days">Down revisions in the last thirty days.</param>
/// <param name="DownLast90Days">Down revisions in the last ninety days.</param>
/// <param name="Currency">Currency.</param>
public sealed record PeriodicEpsRevisions(
    string Period,
    DateOnly? EndDate,
    int? UpLast7Days,
    int? UpLast30Days,
    int? DownLast7Days,
    int? DownLast30Days,
    int? DownLast90Days,
    string? Currency);

/// <summary>
/// Growth estimate comparison row.
/// </summary>
/// <param name="Period">Reporting period label.</param>
/// <param name="Stock">Stock growth estimate.</param>
/// <param name="Industry">Industry growth estimate.</param>
/// <param name="Sector">Sector growth estimate.</param>
/// <param name="Index">Index growth estimate.</param>
public sealed record GrowthEstimate(
    string Period,
    decimal? Stock,
    decimal? Industry,
    decimal? Sector,
    decimal? Index);