namespace YFinance.Net;

/// <summary>
/// Controls how price bars are transformed before they are returned.
/// </summary>
public enum PriceAdjustmentMode
{
    /// <summary>
    /// Return raw prices exactly as reported by Yahoo Finance.
    /// </summary>
    None,

    /// <summary>
    /// Scale open, high, low, and close using the adjusted-close ratio,
    /// then expose the adjusted close as the returned close.
    /// </summary>
    AdjustAll,

    /// <summary>
    /// Scale open, high, and low using the adjusted-close ratio while
    /// preserving the raw close value returned by Yahoo.
    /// </summary>
    AdjustOpenHighLow
}

/// <summary>
/// Controls how history timestamps are shaped before they are returned.
/// </summary>
public enum PriceTimestampMode
{
    /// <summary>
    /// Return timestamps as UTC offsets.
    /// </summary>
    Utc,

    /// <summary>
    /// Convert timestamps to the exchange-local offset using Yahoo's exchange timezone metadata.
    /// </summary>
    ExchangeLocal
}

/// <summary>
/// Request parameters for historical price data for a single symbol.
/// </summary>
public sealed record PriceHistoryRequest
{
    /// <summary>
    /// Symbol to query.
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Yahoo range preset such as <c>1mo</c>, <c>6mo</c>, or <c>1y</c>. Mutually exclusive with <see cref="Start"/> and <see cref="End"/>.
    /// </summary>
    public string? Range { get; init; } = "1mo";

    /// <summary>
    /// Bar interval such as <c>1d</c>, <c>1h</c>, or <c>5m</c>.
    /// </summary>
    public string Interval { get; init; } = "1d";

    /// <summary>
    /// Optional start timestamp for explicit date-range queries.
    /// </summary>
    public DateTimeOffset? Start { get; init; }

    /// <summary>
    /// Optional end timestamp for explicit date-range queries.
    /// </summary>
    public DateTimeOffset? End { get; init; }

    /// <summary>
    /// Gets whether pre-market and after-hours bars should be included when Yahoo supports them.
    /// </summary>
    public bool IncludePrePost { get; init; }

    /// <summary>
    /// Optional post-processing applied to returned price bars.
    /// </summary>
    public PriceAdjustmentMode AdjustmentMode { get; init; } = PriceAdjustmentMode.None;

    /// <summary>
    /// Controls whether returned timestamps stay in UTC or are converted to the exchange-local offset.
    /// </summary>
    public PriceTimestampMode TimestampMode { get; init; } = PriceTimestampMode.Utc;
}

/// <summary>
/// Request parameters for historical price data for multiple symbols.
/// </summary>
public sealed record BatchPriceHistoryRequest
{
    /// <summary>
    /// Symbols to query.
    /// </summary>
    public required IReadOnlyList<string> Symbols { get; init; }

    /// <summary>
    /// Yahoo range preset such as <c>1mo</c>, <c>6mo</c>, or <c>1y</c>. Mutually exclusive with <see cref="Start"/> and <see cref="End"/>.
    /// </summary>
    public string? Range { get; init; } = "1mo";

    /// <summary>
    /// Bar interval such as <c>1d</c>, <c>1h</c>, or <c>5m</c>.
    /// </summary>
    public string Interval { get; init; } = "1d";

    /// <summary>
    /// Optional start timestamp for explicit date-range queries.
    /// </summary>
    public DateTimeOffset? Start { get; init; }

    /// <summary>
    /// Optional end timestamp for explicit date-range queries.
    /// </summary>
    public DateTimeOffset? End { get; init; }

    /// <summary>
    /// Gets whether pre-market and after-hours bars should be included when Yahoo supports them.
    /// </summary>
    public bool IncludePrePost { get; init; }

    /// <summary>
    /// Optional post-processing applied to returned price bars.
    /// </summary>
    public PriceAdjustmentMode AdjustmentMode { get; init; } = PriceAdjustmentMode.None;

    /// <summary>
    /// Controls whether returned timestamps stay in UTC or are converted to the exchange-local offset.
    /// </summary>
    public PriceTimestampMode TimestampMode { get; init; } = PriceTimestampMode.Utc;

    /// <summary>
    /// Maximum number of concurrent symbol requests.
    /// </summary>
    public int MaxConcurrency { get; init; } = 4;
}

/// <summary>
/// Historical price data returned for a symbol.
/// </summary>
/// <param name="Symbol">Symbol used for the request.</param>
/// <param name="Currency">Currency of the returned prices.</param>
/// <param name="ExchangeTimeZone">Exchange time zone reported by Yahoo Finance.</param>
/// <param name="InstrumentType">Instrument type reported by Yahoo Finance.</param>
/// <param name="ValidRanges">Valid range presets supported by Yahoo for the symbol.</param>
/// <param name="Bars">Price bars returned by Yahoo Finance.</param>
/// <param name="Dividends">Dividend events returned alongside the chart data.</param>
/// <param name="Splits">Split events returned alongside the chart data.</param>
/// <param name="CapitalGains">Capital gain events returned alongside the chart data.</param>
public sealed record PriceHistoryResult(
    string Symbol,
    string? Currency,
    string? ExchangeTimeZone,
    string? InstrumentType,
    string[] ValidRanges,
    PriceBar[] Bars,
    DividendEvent[] Dividends,
    SplitEvent[] Splits,
    CapitalGainEvent[] CapitalGains);

/// <summary>
/// Batch historical price data results keyed by symbol.
/// </summary>
/// <param name="Histories">Successful price history results keyed by symbol.</param>
/// <param name="Failures">Failed symbol requests keyed by symbol.</param>
public sealed record BatchPriceHistoryResult(
    IReadOnlyDictionary<string, PriceHistoryResult> Histories,
    IReadOnlyDictionary<string, BatchHistoryFailure> Failures);

/// <summary>
/// Describes a failed price history request in a batch operation.
/// </summary>
/// <param name="Symbol">Symbol that failed.</param>
/// <param name="Message">Failure message captured for the symbol.</param>
public sealed record BatchHistoryFailure(
    string Symbol,
    string Message);

/// <summary>
/// Represents a single OHLCV price bar.
/// </summary>
/// <param name="Timestamp">Timestamp of the bar.</param>
/// <param name="Open">Opening price.</param>
/// <param name="High">High price.</param>
/// <param name="Low">Low price.</param>
/// <param name="Close">Closing price.</param>
/// <param name="AdjustedClose">Adjusted close price when available.</param>
/// <param name="Volume">Trading volume.</param>
public readonly record struct PriceBar(
    DateTimeOffset Timestamp,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal? Close,
    decimal? AdjustedClose,
    long? Volume);

/// <summary>
/// Represents a dividend event attached to chart data.
/// </summary>
/// <param name="Timestamp">Timestamp of the dividend event.</param>
/// <param name="Amount">Dividend amount.</param>
/// <param name="Currency">Currency of the dividend amount.</param>
public readonly record struct DividendEvent(
    DateTimeOffset Timestamp,
    decimal Amount,
    string? Currency);

/// <summary>
/// Represents a stock split event attached to chart data.
/// </summary>
/// <param name="Timestamp">Timestamp of the split event.</param>
/// <param name="Ratio">Split ratio as a decimal value.</param>
/// <param name="Numerator">Split numerator when Yahoo provides it.</param>
/// <param name="Denominator">Split denominator when Yahoo provides it.</param>
public readonly record struct SplitEvent(
    DateTimeOffset Timestamp,
    decimal Ratio,
    long? Numerator,
    long? Denominator);

/// <summary>
/// Represents a capital gain event attached to chart data.
/// </summary>
/// <param name="Timestamp">Timestamp of the capital gain event.</param>
/// <param name="Amount">Capital gain amount.</param>
public readonly record struct CapitalGainEvent(
    DateTimeOffset Timestamp,
    decimal Amount);