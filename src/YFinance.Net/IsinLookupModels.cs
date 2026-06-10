namespace YFinance.Net;

public sealed record IsinSearchResult(
    string Isin,
    SearchQuote? Ticker,
    SearchNewsItem[] News);