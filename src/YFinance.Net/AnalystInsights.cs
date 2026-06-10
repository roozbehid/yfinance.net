namespace YFinance.Net;

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

public sealed record AnalystPriceTargets(
    decimal? Current,
    decimal? Low,
    decimal? High,
    decimal? Mean,
    decimal? Median);

public sealed record RecommendationTrendEntry(
    string Period,
    int? StrongBuy,
    int? Buy,
    int? Hold,
    int? Sell,
    int? StrongSell);

public sealed record UpgradeDowngradeEntry(
    DateTimeOffset? GradeDate,
    string? Firm,
    string? ToGrade,
    string? FromGrade,
    string? Action,
    string? PriceTargetAction,
    decimal? CurrentPriceTarget,
    decimal? PriorPriceTarget);

public sealed record EarningsHistoryEntry(
    string? Period,
    DateTimeOffset? Quarter,
    decimal? EpsActual,
    decimal? EpsEstimate,
    decimal? EpsDifference,
    decimal? SurprisePercent,
    string? Currency);

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

public sealed record PeriodicEpsTrend(
    string Period,
    DateOnly? EndDate,
    decimal? Current,
    decimal? SevenDaysAgo,
    decimal? ThirtyDaysAgo,
    decimal? SixtyDaysAgo,
    decimal? NinetyDaysAgo,
    string? Currency);

public sealed record PeriodicEpsRevisions(
    string Period,
    DateOnly? EndDate,
    int? UpLast7Days,
    int? UpLast30Days,
    int? DownLast7Days,
    int? DownLast30Days,
    int? DownLast90Days,
    string? Currency);

public sealed record GrowthEstimate(
    string Period,
    decimal? Stock,
    decimal? Industry,
    decimal? Sector,
    decimal? Index);