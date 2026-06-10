namespace YFinance.Net;

/// <summary>
/// Configures request behavior for <see cref="YahooFinanceClient"/>.
/// </summary>
public sealed record YahooFinanceClientOptions
{
    /// <summary>
    /// Gets the base URI used for Yahoo Finance HTTP requests.
    /// </summary>
    public Uri BaseUri { get; init; } = new("https://query2.finance.yahoo.com", UriKind.Absolute);

    /// <summary>
    /// Gets the user agent sent with outgoing requests.
    /// </summary>
    public string UserAgent { get; init; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36";

    /// <summary>
    /// Gets the <c>Accept-Language</c> header sent with outgoing requests.
    /// </summary>
    public string AcceptLanguage { get; init; } = "en-US,en;q=0.9";

    /// <summary>
    /// Gets cache configuration used by client features that support caching.
    /// </summary>
    public YahooFinanceCacheOptions Cache { get; init; } = new();
}