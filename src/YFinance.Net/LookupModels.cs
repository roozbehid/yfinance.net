namespace YFinance.Net;

/// <summary>
/// Filters available for Yahoo Finance lookup.
/// </summary>
public enum LookupType
{
    /// <summary>
    /// All supported lookup types.
    /// </summary>
    All,
    /// <summary>
    /// Equities.
    /// </summary>
    Equity,
    /// <summary>
    /// Mutual funds.
    /// </summary>
    MutualFund,
    /// <summary>
    /// Exchange-traded funds.
    /// </summary>
    Etf,
    /// <summary>
    /// Indexes.
    /// </summary>
    Index,
    /// <summary>
    /// Futures.
    /// </summary>
    Future,
    /// <summary>
    /// Currencies.
    /// </summary>
    Currency,
    /// <summary>
    /// Cryptocurrencies.
    /// </summary>
    Cryptocurrency
}

/// <summary>
/// Request parameters for Yahoo Finance lookup.
/// </summary>
public sealed record LookupRequest
{
    /// <summary>
    /// Query text to look up.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Lookup type filter.
    /// </summary>
    public LookupType Type { get; init; } = LookupType.All;

    /// <summary>
    /// Maximum number of documents to return.
    /// </summary>
    public int Count { get; init; } = 25;

    /// <summary>
    /// Result offset for paging.
    /// </summary>
    public int Start { get; init; }

    /// <summary>
    /// Gets whether pricing data should be included when available.
    /// </summary>
    public bool FetchPricingData { get; init; } = true;

    /// <summary>
    /// Language header value used by Yahoo Finance.
    /// </summary>
    public string Language { get; init; } = "en-US";

    /// <summary>
    /// Region code used by Yahoo Finance.
    /// </summary>
    public string Region { get; init; } = "US";
}

/// <summary>
/// Lookup results returned by Yahoo Finance.
/// </summary>
/// <param name="Documents">Matching lookup documents.</param>
public sealed record LookupResult(LookupDocument[] Documents);

/// <summary>
/// Single lookup document returned by Yahoo Finance.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="CompanyName">Company or instrument name.</param>
/// <param name="Exchange">Exchange code.</param>
/// <param name="Type">Yahoo lookup type.</param>
/// <param name="Score">Relevance score as returned by Yahoo Finance.</param>
/// <param name="Price">Price when pricing data is requested and available.</param>
public readonly record struct LookupDocument(
    string Symbol,
    string? CompanyName,
    string? Exchange,
    string? Type,
    string? Score,
    decimal? Price);