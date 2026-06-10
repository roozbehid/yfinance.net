namespace YFinance.Net;

public sealed record OptionChainResult(
    string Symbol,
    DateOnly[] ExpirationDates,
    DateOnly? ExpirationDate,
    OptionContract[] Calls,
    OptionContract[] Puts,
    OptionUnderlyingQuote? Underlying);

public readonly record struct OptionContract(
    string ContractSymbol,
    DateTimeOffset? LastTradeDate,
    decimal? Strike,
    decimal? LastPrice,
    decimal? Bid,
    decimal? Ask,
    decimal? Change,
    decimal? PercentChange,
    long? Volume,
    long? OpenInterest,
    decimal? ImpliedVolatility,
    bool? InTheMoney,
    string? ContractSize,
    string? Currency);

public readonly record struct OptionUnderlyingQuote(
    string Symbol,
    string? ShortName,
    string? LongName,
    string? QuoteType,
    string? Exchange,
    string? Currency,
    decimal? RegularMarketPrice,
    decimal? RegularMarketChange,
    decimal? RegularMarketChangePercent,
    DateTimeOffset? RegularMarketTime,
    string? MarketState);