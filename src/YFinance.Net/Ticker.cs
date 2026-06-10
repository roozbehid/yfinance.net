using System.Globalization;

namespace YFinance.Net;

/// <summary>
/// Convenience facade for symbol-centric operations against a single ticker.
/// </summary>
public sealed class Ticker : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    /// <summary>
    /// Initializes a ticker facade for the specified symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol represented by this facade.</param>
    /// <param name="client">Optional shared client instance. When omitted, the ticker owns a new <see cref="YahooFinanceClient"/>.</param>
    public Ticker(string symbol, YahooFinanceClient? client = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        Symbol = symbol;
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    /// <summary>
    /// Gets the symbol represented by this ticker facade.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets quote summary data for <see cref="Symbol"/>.
    /// </summary>
    public Task<QuoteSummary> GetQuoteSummaryAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetQuoteSummaryAsync(Symbol, cancellationToken);
    }

    /// <summary>
    /// Gets company profile and key statistics for <see cref="Symbol"/>.
    /// </summary>
    public Task<CompanyProfile> GetCompanyProfileAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetCompanyProfileAsync(Symbol, cancellationToken);
    }

    /// <summary>
    /// Gets holder information for <see cref="Symbol"/>.
    /// </summary>
    public Task<HoldersSnapshot> GetHoldersAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetHoldersAsync(Symbol, cancellationToken);
    }

    /// <summary>
    /// Gets insider activity for <see cref="Symbol"/>.
    /// </summary>
    public Task<InsiderSnapshot> GetInsiderDataAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetInsiderDataAsync(Symbol, cancellationToken);
    }

    /// <summary>
    /// Gets valuation measures for <see cref="Symbol"/>.
    /// </summary>
    public Task<ValuationMeasures> GetValuationMeasuresAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetValuationMeasuresAsync(Symbol, cancellationToken);
    }

    /// <summary>
    /// Attempts to resolve an ISIN for <see cref="Symbol"/>.
    /// </summary>
    public Task<string?> GetIsinAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetIsinAsync(Symbol, cancellationToken);
    }

    /// <summary>
    /// Gets an income statement for <see cref="Symbol"/>.
    /// </summary>
    public Task<IncomeStatement> GetIncomeStatementAsync(
        FinancialStatementFrequency frequency = FinancialStatementFrequency.Annual,
        CancellationToken cancellationToken = default)
    {
        return _client.GetIncomeStatementAsync(Symbol, frequency, cancellationToken);
    }

    /// <summary>
    /// Gets the trailing twelve month income statement for <see cref="Symbol"/>.
    /// </summary>
    public Task<IncomeStatement> GetTrailingIncomeStatementAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetTrailingIncomeStatementAsync(Symbol, cancellationToken);
    }

    /// <summary>
    /// Gets a balance sheet for <see cref="Symbol"/>.
    /// </summary>
    public Task<BalanceSheet> GetBalanceSheetAsync(
        FinancialStatementFrequency frequency = FinancialStatementFrequency.Annual,
        CancellationToken cancellationToken = default)
    {
        return _client.GetBalanceSheetAsync(Symbol, frequency, cancellationToken);
    }

    /// <summary>
    /// Gets a cash flow statement for <see cref="Symbol"/>.
    /// </summary>
    public Task<CashFlow> GetCashFlowAsync(
        FinancialStatementFrequency frequency = FinancialStatementFrequency.Annual,
        CancellationToken cancellationToken = default)
    {
        return _client.GetCashFlowAsync(Symbol, frequency, cancellationToken);
    }

    /// <summary>
    /// Gets ticker news for <see cref="Symbol"/>.
    /// </summary>
    public Task<TickerNewsItem[]> GetNewsAsync(int count = 10, TickerNewsTab tab = TickerNewsTab.News, CancellationToken cancellationToken = default)
    {
        return _client.GetNewsAsync(Symbol, count, tab, cancellationToken);
    }

    /// <summary>
    /// Gets available option expiration dates for <see cref="Symbol"/>.
    /// </summary>
    public Task<DateOnly[]> GetOptionExpirationDatesAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetOptionExpirationDatesAsync(Symbol, cancellationToken);
    }

    /// <summary>
    /// Gets available option expiration dates for <see cref="Symbol"/>, controlling cache behavior.
    /// </summary>
    public Task<DateOnly[]> GetOptionExpirationDatesAsync(YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        return _client.GetOptionExpirationDatesAsync(Symbol, cacheMode, cancellationToken);
    }

    /// <summary>
    /// Gets the option chain for <see cref="Symbol"/>.
    /// </summary>
    public Task<OptionChainResult> GetOptionChainAsync(DateOnly? expirationDate = null, CancellationToken cancellationToken = default)
    {
        return _client.GetOptionChainAsync(Symbol, expirationDate, cancellationToken);
    }

    /// <summary>
    /// Gets the option chain for <see cref="Symbol"/>, controlling cache behavior.
    /// </summary>
    public Task<OptionChainResult> GetOptionChainAsync(DateOnly? expirationDate, YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        return _client.GetOptionChainAsync(Symbol, expirationDate, cacheMode, cancellationToken);
    }

    /// <summary>
    /// Gets analyst insights for <see cref="Symbol"/>.
    /// </summary>
    public Task<AnalystInsights> GetAnalystInsightsAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetAnalystInsightsAsync(Symbol, cancellationToken);
    }

    /// <summary>
    /// Gets historical or upcoming earnings dates for <see cref="Symbol"/>.
    /// </summary>
    public Task<EarningsDatesResult> GetEarningsDatesAsync(int limit = 12, int offset = 0, CancellationToken cancellationToken = default)
    {
        return _client.GetEarningsDatesAsync(Symbol, limit, offset, cancellationToken);
    }

    /// <summary>
    /// Gets historical price data for <see cref="Symbol"/>.
    /// </summary>
    public Task<PriceHistoryResult> GetHistoryAsync(
        string range = "1mo",
        string interval = "1d",
        bool includePrePost = false,
        PriceAdjustmentMode adjustmentMode = PriceAdjustmentMode.None,
        PriceTimestampMode timestampMode = PriceTimestampMode.Utc,
        CancellationToken cancellationToken = default)
    {
        return _client.GetPriceHistoryAsync(new PriceHistoryRequest
        {
            Symbol = Symbol,
            Range = range,
            Interval = interval,
            IncludePrePost = includePrePost,
            AdjustmentMode = adjustmentMode,
            TimestampMode = timestampMode
        }, cancellationToken);
    }

    /// <summary>
    /// Disposes the owned client when this ticker created it.
    /// </summary>
    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }
}