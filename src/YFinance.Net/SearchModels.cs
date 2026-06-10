namespace YFinance.Net;

/// <summary>
/// Request parameters for Yahoo Finance search.
/// </summary>
public sealed record SearchRequest
{
    /// <summary>
    /// Search text sent to Yahoo Finance.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Maximum number of quote matches to return.
    /// </summary>
    public int QuotesCount { get; init; } = 8;

    /// <summary>
    /// Maximum number of news items to return.
    /// </summary>
    public int NewsCount { get; init; } = 8;

    /// <summary>
    /// Maximum number of list results to return.
    /// </summary>
    public int ListsCount { get; init; } = 8;

    /// <summary>
    /// Gets whether Yahoo should include company breakdown data.
    /// </summary>
    public bool IncludeCompanyBreakdown { get; init; } = true;

    /// <summary>
    /// Gets whether Yahoo should include navigation links.
    /// </summary>
    public bool IncludeNavigationLinks { get; init; }

    /// <summary>
    /// Gets whether Yahoo should include research reports.
    /// </summary>
    public bool IncludeResearchReports { get; init; }

    /// <summary>
    /// Gets whether Yahoo should include cultural assets.
    /// </summary>
    public bool IncludeCulturalAssets { get; init; }

    /// <summary>
    /// Gets whether fuzzy matching should be enabled.
    /// </summary>
    public bool EnableFuzzyQuery { get; init; }

    /// <summary>
    /// Maximum number of recommended items to return.
    /// </summary>
    public int RecommendedCount { get; init; } = 8;
}

/// <summary>
/// Search results returned by Yahoo Finance.
/// </summary>
/// <param name="Quotes">Quote matches returned by the search.</param>
/// <param name="News">News items returned by the search.</param>
/// <param name="ListCount">Number of list results reported by Yahoo Finance.</param>
/// <param name="ResearchReportCount">Number of research reports reported by Yahoo Finance.</param>
/// <param name="NavigationLinkCount">Number of navigation links reported by Yahoo Finance.</param>
public sealed record SearchResult(
    SearchQuote[] Quotes,
    SearchNewsItem[] News,
    int ListCount,
    int ResearchReportCount,
    int NavigationLinkCount);

/// <summary>
/// Quote match returned from Yahoo Finance search.
/// </summary>
/// <param name="Symbol">Ticker symbol.</param>
/// <param name="ShortName">Short display name.</param>
/// <param name="LongName">Long display name.</param>
/// <param name="QuoteType">Quote type.</param>
/// <param name="Exchange">Exchange code.</param>
/// <param name="ExchangeDisplayName">Human-readable exchange name.</param>
public readonly record struct SearchQuote(
    string Symbol,
    string? ShortName,
    string? LongName,
    string? QuoteType,
    string? Exchange,
    string? ExchangeDisplayName);

/// <summary>
/// News item returned from Yahoo Finance search.
/// </summary>
/// <param name="Id">Yahoo news item identifier.</param>
/// <param name="Title">News headline.</param>
/// <param name="Publisher">News publisher.</param>
public readonly record struct SearchNewsItem(
    string? Id,
    string? Title,
    string? Publisher);