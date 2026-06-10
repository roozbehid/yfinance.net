namespace YFinance.Net;

public sealed record SearchRequest
{
    public required string Query { get; init; }

    public int QuotesCount { get; init; } = 8;

    public int NewsCount { get; init; } = 8;

    public int ListsCount { get; init; } = 8;

    public bool IncludeCompanyBreakdown { get; init; } = true;

    public bool IncludeNavigationLinks { get; init; }

    public bool IncludeResearchReports { get; init; }

    public bool IncludeCulturalAssets { get; init; }

    public bool EnableFuzzyQuery { get; init; }

    public int RecommendedCount { get; init; } = 8;
}

public sealed record SearchResult(
    SearchQuote[] Quotes,
    SearchNewsItem[] News,
    int ListCount,
    int ResearchReportCount,
    int NavigationLinkCount);

public readonly record struct SearchQuote(
    string Symbol,
    string? ShortName,
    string? LongName,
    string? QuoteType,
    string? Exchange,
    string? ExchangeDisplayName);

public readonly record struct SearchNewsItem(
    string? Id,
    string? Title,
    string? Publisher);