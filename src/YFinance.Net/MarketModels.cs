namespace YFinance.Net;

public enum MarketRegion
{
    US,
    GB,
    ASIA,
    EUROPE,
    RATES,
    COMMODITIES,
    CURRENCIES,
    CRYPTOCURRENCIES
}

public sealed record MarketSummaryResult(
    MarketRegion Region,
    string? CategoryName,
    IReadOnlyDictionary<string, MarketSummaryEntry> Exchanges);

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