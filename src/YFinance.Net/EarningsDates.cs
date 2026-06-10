namespace YFinance.Net;

/// <summary>
/// Earnings dates returned for a symbol.
/// </summary>
/// <param name="Total">Total number of matching rows reported by Yahoo Finance.</param>
/// <param name="Entries">Earnings date entries returned for the current page.</param>
public sealed record EarningsDatesResult(
    int Total,
    IReadOnlyList<EarningsDateEntry> Entries);

/// <summary>
/// Single earnings date row returned by Yahoo Finance.
/// </summary>
/// <param name="EarningsDate">Earnings date and time.</param>
/// <param name="TimeZoneShortName">Time zone abbreviation returned by Yahoo Finance.</param>
/// <param name="EpsEstimate">EPS estimate.</param>
/// <param name="ReportedEps">Reported EPS.</param>
/// <param name="SurprisePercent">Surprise percentage.</param>
/// <param name="EventType">Event type label.</param>
public readonly record struct EarningsDateEntry(
    DateTimeOffset? EarningsDate,
    string? TimeZoneShortName,
    decimal? EpsEstimate,
    decimal? ReportedEps,
    decimal? SurprisePercent,
    string? EventType);