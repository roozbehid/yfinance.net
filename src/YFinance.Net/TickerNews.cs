namespace YFinance.Net;

public enum TickerNewsTab
{
    News,
    All,
    PressReleases
}

public sealed record BatchTickerNewsRequest(
    IReadOnlyList<string> Symbols,
    int Count = 10,
    TickerNewsTab Tab = TickerNewsTab.News,
    int MaxConcurrency = 4);

public sealed record BatchTickerNewsResult(
    IReadOnlyDictionary<string, TickerNewsItem[]> NewsBySymbol,
    IReadOnlyDictionary<string, BatchTickerNewsFailure> Failures);

public sealed record BatchTickerNewsFailure(
    string Symbol,
    string Message);

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