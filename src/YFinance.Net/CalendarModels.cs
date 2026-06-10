namespace YFinance.Net;

/// <summary>
/// Paged calendar results returned by Yahoo Finance.
/// </summary>
/// <param name="Total">Total number of matching rows reported by Yahoo Finance.</param>
/// <param name="Entries">Entries returned for the current page.</param>
public sealed record CalendarResult<TEntry>(
    int Total,
    IReadOnlyList<TEntry> Entries);

/// <summary>
/// Request parameters for the Yahoo Finance earnings calendar.
/// </summary>
public sealed record EarningsCalendarRequest(
    DateOnly? Start = null,
    DateOnly? End = null,
    int Limit = 12,
    int Offset = 0,
    decimal? MinimumMarketCap = null);

/// <summary>
/// Request parameters for the Yahoo Finance IPO calendar.
/// </summary>
public sealed record IpoCalendarRequest(
    DateOnly? Start = null,
    DateOnly? End = null,
    int Limit = 12,
    int Offset = 0);

/// <summary>
/// Request parameters for the Yahoo Finance economic events calendar.
/// </summary>
public sealed record EconomicEventCalendarRequest(
    DateOnly? Start = null,
    DateOnly? End = null,
    int Limit = 12,
    int Offset = 0);

/// <summary>
/// Request parameters for the Yahoo Finance stock splits calendar.
/// </summary>
public sealed record SplitsCalendarRequest(
    DateOnly? Start = null,
    DateOnly? End = null,
    int Limit = 12,
    int Offset = 0);

/// <summary>
/// Earnings calendar entry returned by Yahoo Finance.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="Company">Company name.</param>
/// <param name="MarketCap">Market capitalization.</param>
/// <param name="EventName">Event name reported by Yahoo Finance.</param>
/// <param name="EventStartDate">Event start date and time.</param>
/// <param name="Timing">Timing label such as before or after market.</param>
/// <param name="EpsEstimate">EPS estimate.</param>
/// <param name="ReportedEps">Reported EPS.</param>
/// <param name="SurprisePercent">Surprise percentage.</param>
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

/// <summary>
/// IPO calendar entry returned by Yahoo Finance.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="Company">Company name.</param>
/// <param name="Exchange">Exchange name or code.</param>
/// <param name="FilingDate">Filing date.</param>
/// <param name="Date">Planned or effective IPO date.</param>
/// <param name="AmendedDate">Latest amended date.</param>
/// <param name="PriceFrom">Lower bound of the offering price range.</param>
/// <param name="PriceTo">Upper bound of the offering price range.</param>
/// <param name="Price">Final offering price when available.</param>
/// <param name="Currency">Offering currency.</param>
/// <param name="Shares">Number of shares in the offering.</param>
/// <param name="Action">Action or status text reported by Yahoo Finance.</param>
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

/// <summary>
/// Economic event calendar entry returned by Yahoo Finance.
/// </summary>
/// <param name="Event">Economic event name.</param>
/// <param name="CountryCode">Country code associated with the event.</param>
/// <param name="EventTime">Event timestamp.</param>
/// <param name="Period">Reporting period label.</param>
/// <param name="Actual">Actual reported value.</param>
/// <param name="Expected">Consensus or expected value.</param>
/// <param name="Last">Prior released value.</param>
/// <param name="Revised">Revised prior value.</param>
public sealed record EconomicEventCalendarEntry(
    string Event,
    string? CountryCode,
    DateTimeOffset? EventTime,
    string? Period,
    string? Actual,
    string? Expected,
    string? Last,
    string? Revised);

/// <summary>
/// Stock split calendar entry returned by Yahoo Finance.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="Company">Company name.</param>
/// <param name="PayableOn">Payable date of the split.</param>
/// <param name="Optionable">Indicates whether the security is optionable.</param>
/// <param name="OldShareWorth">Old share worth value reported by Yahoo Finance.</param>
/// <param name="ShareWorth">New share worth value reported by Yahoo Finance.</param>
public sealed record SplitCalendarEntry(
    string Symbol,
    string? Company,
    DateTimeOffset? PayableOn,
    bool? Optionable,
    decimal? OldShareWorth,
    decimal? ShareWorth);