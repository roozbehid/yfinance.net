namespace YFinance.Net;

/// <summary>
/// Represents an option chain returned by Yahoo Finance for a symbol.
/// </summary>
/// <param name="Symbol">Underlying ticker symbol for the option chain.</param>
/// <param name="ExpirationDates">Available expiration dates returned by Yahoo Finance.</param>
/// <param name="ExpirationDate">Expiration date for the returned chain when a specific expiry was requested.</param>
/// <param name="Calls">Call contracts in the returned chain.</param>
/// <param name="Puts">Put contracts in the returned chain.</param>
/// <param name="Underlying">Underlying quote metadata included alongside the option chain.</param>
public sealed record OptionChainResult(
    string Symbol,
    DateOnly[] ExpirationDates,
    DateOnly? ExpirationDate,
    OptionContract[] Calls,
    OptionContract[] Puts,
    OptionUnderlyingQuote? Underlying);

/// <summary>
/// Represents a single option contract in a Yahoo Finance option chain.
/// </summary>
/// <param name="ContractSymbol">Unique contract symbol reported by Yahoo Finance.</param>
/// <param name="LastTradeDate">Timestamp of the most recent trade.</param>
/// <param name="Strike">Strike price of the contract.</param>
/// <param name="LastPrice">Last traded price.</param>
/// <param name="Bid">Current bid price.</param>
/// <param name="Ask">Current ask price.</param>
/// <param name="Change">Absolute price change for the session.</param>
/// <param name="PercentChange">Percentage price change for the session.</param>
/// <param name="Volume">Session trading volume.</param>
/// <param name="OpenInterest">Open interest for the contract.</param>
/// <param name="ImpliedVolatility">Implied volatility reported by Yahoo Finance.</param>
/// <param name="InTheMoney">Indicates whether the contract is currently in the money.</param>
/// <param name="ContractSize">Contract size label such as <c>REGULAR</c>.</param>
/// <param name="Currency">Currency of the contract prices.</param>
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

/// <summary>
/// Represents the underlying quote snapshot included with an option chain.
/// </summary>
/// <param name="Symbol">Underlying ticker symbol.</param>
/// <param name="ShortName">Short display name of the underlying instrument.</param>
/// <param name="LongName">Long display name of the underlying instrument.</param>
/// <param name="QuoteType">Yahoo quote type of the underlying instrument.</param>
/// <param name="Exchange">Exchange name reported by Yahoo Finance.</param>
/// <param name="Currency">Currency of the underlying quote.</param>
/// <param name="RegularMarketPrice">Current regular market price.</param>
/// <param name="RegularMarketChange">Absolute regular market price change.</param>
/// <param name="RegularMarketChangePercent">Percentage regular market price change.</param>
/// <param name="RegularMarketTime">Timestamp of the regular market quote.</param>
/// <param name="MarketState">Market state such as <c>REGULAR</c> or <c>POST</c>.</param>
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