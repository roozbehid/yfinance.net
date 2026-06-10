namespace YFinance.Net;

/// <summary>
/// Basic quote summary fields returned by Yahoo Finance for a symbol.
/// </summary>
/// <param name="Symbol">Symbol used for the request.</param>
/// <param name="ShortName">Short display name of the instrument.</param>
/// <param name="LongName">Long display name of the instrument.</param>
/// <param name="QuoteType">Quote type reported by Yahoo Finance.</param>
/// <param name="Exchange">Exchange name reported by Yahoo Finance.</param>
/// <param name="Currency">Currency of the quote.</param>
/// <param name="RegularMarketPrice">Current regular market price.</param>
/// <param name="PreviousClose">Previous session close.</param>
/// <param name="Open">Current session open.</param>
/// <param name="DayHigh">Current session high.</param>
/// <param name="DayLow">Current session low.</param>
/// <param name="Volume">Current session volume.</param>
public sealed record QuoteSummary(
    string Symbol,
    string? ShortName,
    string? LongName,
    string? QuoteType,
    string? Exchange,
    string? Currency,
    decimal? RegularMarketPrice,
    decimal? PreviousClose,
    decimal? Open,
    decimal? DayHigh,
    decimal? DayLow,
    long? Volume);