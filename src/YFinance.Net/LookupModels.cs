namespace YFinance.Net;

public enum LookupType
{
    All,
    Equity,
    MutualFund,
    Etf,
    Index,
    Future,
    Currency,
    Cryptocurrency
}

public sealed record LookupRequest
{
    public required string Query { get; init; }

    public LookupType Type { get; init; } = LookupType.All;

    public int Count { get; init; } = 25;

    public int Start { get; init; }

    public bool FetchPricingData { get; init; } = true;

    public string Language { get; init; } = "en-US";

    public string Region { get; init; } = "US";
}

public sealed record LookupResult(LookupDocument[] Documents);

public readonly record struct LookupDocument(
    string Symbol,
    string? CompanyName,
    string? Exchange,
    string? Type,
    string? Score,
    decimal? Price);