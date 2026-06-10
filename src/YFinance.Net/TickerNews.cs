namespace YFinance.Net;

/// <summary>
/// Yahoo Finance news tabs for ticker news queries.
/// </summary>
public enum TickerNewsTab
{
    /// <summary>
    /// News tab.
    /// </summary>
    News,
    /// <summary>
    /// All content.
    /// </summary>
    All,
    /// <summary>
    /// Press releases only.
    /// </summary>
    PressReleases
}

/// <summary>
/// Batch request for ticker news.
/// </summary>
/// <param name="Symbols">Symbols to query.</param>
/// <param name="Count">Maximum number of news items per symbol.</param>
/// <param name="Tab">News tab to query.</param>
/// <param name="MaxConcurrency">Maximum number of concurrent symbol requests.</param>
public sealed record BatchTickerNewsRequest(
    IReadOnlyList<string> Symbols,
    int Count = 10,
    TickerNewsTab Tab = TickerNewsTab.News,
    int MaxConcurrency = 4);

/// <summary>
/// Batch ticker news results keyed by symbol.
/// </summary>
/// <param name="NewsBySymbol">Successful news results keyed by symbol.</param>
/// <param name="Failures">Failed symbol requests keyed by symbol.</param>
public sealed record BatchTickerNewsResult(
    IReadOnlyDictionary<string, TickerNewsItem[]> NewsBySymbol,
    IReadOnlyDictionary<string, BatchTickerNewsFailure> Failures);

/// <summary>
/// Describes a failed ticker news request in a batch operation.
/// </summary>
/// <param name="Symbol">Symbol that failed.</param>
/// <param name="Message">Failure message captured for the symbol.</param>
public sealed record BatchTickerNewsFailure(
    string Symbol,
    string Message);

/// <summary>
/// Ticker news item returned by Yahoo Finance.
/// </summary>
/// <param name="Id">News item identifier.</param>
/// <param name="ContentType">Content type.</param>
/// <param name="Title">Headline.</param>
/// <param name="Summary">Summary text.</param>
/// <param name="PublishedAt">Publication timestamp.</param>
/// <param name="Provider">Content provider.</param>
/// <param name="CanonicalUrl">Canonical URL.</param>
/// <param name="ClickThroughUrl">Click-through URL.</param>
/// <param name="ThumbnailUrl">Thumbnail URL.</param>
/// <param name="IsHosted">Indicates whether the content is hosted by Yahoo.</param>
/// <param name="EditorsPick">Indicates whether the item is an editor's pick.</param>
public readonly record struct TickerNewsItem(
    string? Id,
    string? ContentType,
    string? Title,
    string? Summary,
    DateTimeOffset? PublishedAt,
    string? Provider,
    string? CanonicalUrl,
    string? ClickThroughUrl,
    string? ThumbnailUrl,
    bool IsHosted,
    bool EditorsPick);