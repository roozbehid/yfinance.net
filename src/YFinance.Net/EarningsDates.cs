namespace YFinance.Net;

public sealed record EarningsDatesResult(
    int Total,
    IReadOnlyList<EarningsDateEntry> Entries);

public readonly record struct EarningsDateEntry(
    DateTimeOffset? EarningsDate,
    string? TimeZoneShortName,
    decimal? EpsEstimate,
    decimal? ReportedEps,
    decimal? SurprisePercent,
    string? EventType);