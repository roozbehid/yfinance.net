namespace YFinance.Net;

public sealed record CalendarResult<TEntry>(
    int Total,
    IReadOnlyList<TEntry> Entries);

public sealed record EarningsCalendarRequest(
    DateOnly? Start = null,
    DateOnly? End = null,
    int Limit = 12,
    int Offset = 0,
    decimal? MinimumMarketCap = null);

public sealed record IpoCalendarRequest(
    DateOnly? Start = null,
    DateOnly? End = null,
    int Limit = 12,
    int Offset = 0);

public sealed record EconomicEventCalendarRequest(
    DateOnly? Start = null,
    DateOnly? End = null,
    int Limit = 12,
    int Offset = 0);

public sealed record SplitsCalendarRequest(
    DateOnly? Start = null,
    DateOnly? End = null,
    int Limit = 12,
    int Offset = 0);

public sealed record EarningsCalendarEntry(
    string Symbol,
    string? Company,
    decimal? MarketCap,
    string? EventName,
    DateTimeOffset? EventStartDate,
    string? Timing,
    decimal? EpsEstimate,
    decimal? ReportedEps,
    decimal? SurprisePercent);

public sealed record IpoCalendarEntry(
    string Symbol,
    string? Company,
    string? Exchange,
    DateTimeOffset? FilingDate,
    DateTimeOffset? Date,
    DateTimeOffset? AmendedDate,
    decimal? PriceFrom,
    decimal? PriceTo,
    decimal? Price,
    string? Currency,
    decimal? Shares,
    string? Action);

public sealed record EconomicEventCalendarEntry(
    string Event,
    string? CountryCode,
    DateTimeOffset? EventTime,
    string? Period,
    string? Actual,
    string? Expected,
    string? Last,
    string? Revised);

public sealed record SplitCalendarEntry(
    string Symbol,
    string? Company,
    DateTimeOffset? PayableOn,
    bool? Optionable,
    decimal? OldShareWorth,
    decimal? ShareWorth);