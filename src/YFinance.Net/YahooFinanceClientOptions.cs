namespace YFinance.Net;

public sealed record YahooFinanceClientOptions
{
    public Uri BaseUri { get; init; } = new("https://query2.finance.yahoo.com", UriKind.Absolute);

    public string UserAgent { get; init; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36";

    public string AcceptLanguage { get; init; } = "en-US,en;q=0.9";

    public YahooFinanceCacheOptions Cache { get; init; } = new();
}