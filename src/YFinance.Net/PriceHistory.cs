namespace YFinance.Net;

/// <summary>
/// Controls how price bars are transformed before they are returned.
/// </summary>
public enum PriceAdjustmentMode
{
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

public sealed record PriceHistoryRequest
{
    public required string Symbol { get; init; }

    public string? Range { get; init; } = "1mo";

    public string Interval { get; init; } = "1d";

    public DateTimeOffset? Start { get; init; }

    public DateTimeOffset? End { get; init; }

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

public sealed record BatchPriceHistoryRequest
{
    public required IReadOnlyList<string> Symbols { get; init; }

    public string? Range { get; init; } = "1mo";

    public string Interval { get; init; } = "1d";

    public DateTimeOffset? Start { get; init; }

    public DateTimeOffset? End { get; init; }

    public bool IncludePrePost { get; init; }

    public PriceAdjustmentMode AdjustmentMode { get; init; } = PriceAdjustmentMode.None;

    public PriceTimestampMode TimestampMode { get; init; } = PriceTimestampMode.Utc;

    public int MaxConcurrency { get; init; } = 4;
}

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

public sealed record BatchPriceHistoryResult(
    IReadOnlyDictionary<string, PriceHistoryResult> Histories,
    IReadOnlyDictionary<string, BatchHistoryFailure> Failures);

public sealed record BatchHistoryFailure(
    string Symbol,
    string Message);

public readonly record struct PriceBar(
    DateTimeOffset Timestamp,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal? Close,
    decimal? AdjustedClose,
    long? Volume);

public readonly record struct DividendEvent(
    DateTimeOffset Timestamp,
    decimal Amount,
    string? Currency);

public readonly record struct SplitEvent(
    DateTimeOffset Timestamp,
    decimal Ratio,
    long? Numerator,
    long? Denominator);

public readonly record struct CapitalGainEvent(
    DateTimeOffset Timestamp,
    decimal Amount);