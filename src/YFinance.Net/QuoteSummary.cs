namespace YFinance.Net;

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