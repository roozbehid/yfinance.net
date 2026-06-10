namespace YFinance.Net;

/// <summary>
/// Aggregated result of searching Yahoo Finance by ISIN.
/// </summary>
/// <param name="Isin">ISIN used for the search.</param>
/// <param name="Ticker">First matching quote returned by Yahoo Finance.</param>
/// <param name="News">News items returned alongside the ISIN search.</param>
public sealed record IsinSearchResult(
    string Isin,
    SearchQuote? Ticker,
    SearchNewsItem[] News);