namespace YFinance.Net;

/// <summary>
/// Supported Yahoo Finance market regions for market summary and status endpoints.
/// </summary>
public enum MarketRegion
{
    /// <summary>
    /// United States markets.
    /// </summary>
    US,
    /// <summary>
    /// Great Britain markets.
    /// </summary>
    GB,
    /// <summary>
    /// Asian markets.
    /// </summary>
    ASIA,
    /// <summary>
    /// European markets.
    /// </summary>
    EUROPE,
    /// <summary>
    /// Rates markets.
    /// </summary>
    RATES,
    /// <summary>
    /// Commodities markets.
    /// </summary>
    COMMODITIES,
    /// <summary>
    /// Currency markets.
    /// </summary>
    CURRENCIES,
    /// <summary>
    /// Cryptocurrency markets.
    /// </summary>
    CRYPTOCURRENCIES
}

/// <summary>
/// Market summary data returned for a Yahoo Finance region.
/// </summary>
/// <param name="Region">Region used for the request.</param>
/// <param name="CategoryName">Category name returned by Yahoo Finance for the summary response.</param>
/// <param name="Exchanges">Exchanges returned in the summary, keyed by exchange code.</param>
public sealed record MarketSummaryResult(
    MarketRegion Region,
    string? CategoryName,
    IReadOnlyDictionary<string, MarketSummaryEntry> Exchanges);

/// <summary>
/// Summary data for a single exchange within a market summary response.
/// </summary>
/// <param name="Exchange">Exchange code used as the key in the market summary response.</param>
/// <param name="Symbol">Representative symbol returned for the exchange.</param>
/// <param name="ShortName">Short display name for the exchange entry.</param>
/// <param name="FullExchangeName">Full exchange display name.</param>
/// <param name="QuoteType">Quote type of the exchange entry.</param>
/// <param name="MarketState">Current market state such as <c>REGULAR</c> or <c>CLOSED</c>.</param>
/// <param name="ExchangeTimezoneName">Time zone name for the exchange.</param>
/// <param name="ExchangeTimezoneShortName">Short time zone abbreviation for the exchange.</param>
/// <param name="RegularMarketPrice">Current regular market price.</param>
/// <param name="RegularMarketChange">Absolute regular market change.</param>
/// <param name="RegularMarketChangePercent">Percentage regular market change.</param>
public readonly record struct MarketSummaryEntry(
    string Exchange,
    string? Symbol,
    string? ShortName,
    string? FullExchangeName,
    string? QuoteType,
    string? MarketState,
    string? ExchangeTimezoneName,
    string? ExchangeTimezoneShortName,
    decimal? RegularMarketPrice,
    decimal? RegularMarketChange,
    decimal? RegularMarketChangePercent);

/// <summary>
/// Current market status details for a Yahoo Finance region.
/// </summary>
/// <param name="Region">Region used for the request.</param>
/// <param name="Id">Region identifier returned by Yahoo Finance.</param>
/// <param name="Name">Human-readable market name.</param>
/// <param name="Status">High-level market status text.</param>
/// <param name="Message">Additional status message returned by Yahoo Finance.</param>
/// <param name="YfitMarketId">Yahoo internal market identifier.</param>
/// <param name="YfitMarketStatus">Yahoo internal market status value.</param>
/// <param name="Open">Scheduled market open time.</param>
/// <param name="Close">Scheduled market close time.</param>
/// <param name="TimezoneName">Market time zone name.</param>
/// <param name="TimezoneShortName">Market time zone abbreviation.</param>
/// <param name="UtcOffset">UTC offset reported for the market.</param>
public readonly record struct MarketStatus(
    MarketRegion Region,
    string Id,
    string? Name,
    string? Status,
    string? Message,
    string? YfitMarketId,
    string? YfitMarketStatus,
    DateTimeOffset? Open,
    DateTimeOffset? Close,
    string? TimezoneName,
    string? TimezoneShortName,
    TimeSpan? UtcOffset);