using System.Globalization;

namespace YFinance.Net;

public sealed class Ticker : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    public Ticker(string symbol, YahooFinanceClient? client = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        Symbol = symbol;
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    public string Symbol { get; }

    public Task<QuoteSummary> GetQuoteSummaryAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetQuoteSummaryAsync(Symbol, cancellationToken);
    }

    public Task<CompanyProfile> GetCompanyProfileAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetCompanyProfileAsync(Symbol, cancellationToken);
    }

    public Task<HoldersSnapshot> GetHoldersAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetHoldersAsync(Symbol, cancellationToken);
    }

    public Task<InsiderSnapshot> GetInsiderDataAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetInsiderDataAsync(Symbol, cancellationToken);
    }

    public Task<ValuationMeasures> GetValuationMeasuresAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetValuationMeasuresAsync(Symbol, cancellationToken);
    }

    public Task<string?> GetIsinAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetIsinAsync(Symbol, cancellationToken);
    }

    public Task<IncomeStatement> GetIncomeStatementAsync(
        FinancialStatementFrequency frequency = FinancialStatementFrequency.Annual,
        CancellationToken cancellationToken = default)
    {
        return _client.GetIncomeStatementAsync(Symbol, frequency, cancellationToken);
    }

    public Task<IncomeStatement> GetTrailingIncomeStatementAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetTrailingIncomeStatementAsync(Symbol, cancellationToken);
    }

    public Task<BalanceSheet> GetBalanceSheetAsync(
        FinancialStatementFrequency frequency = FinancialStatementFrequency.Annual,
        CancellationToken cancellationToken = default)
    {
        return _client.GetBalanceSheetAsync(Symbol, frequency, cancellationToken);
    }

    public Task<CashFlow> GetCashFlowAsync(
        FinancialStatementFrequency frequency = FinancialStatementFrequency.Annual,
        CancellationToken cancellationToken = default)
    {
        return _client.GetCashFlowAsync(Symbol, frequency, cancellationToken);
    }

    public Task<TickerNewsItem[]> GetNewsAsync(int count = 10, TickerNewsTab tab = TickerNewsTab.News, CancellationToken cancellationToken = default)
    {
        return _client.GetNewsAsync(Symbol, count, tab, cancellationToken);
    }

    public Task<DateOnly[]> GetOptionExpirationDatesAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetOptionExpirationDatesAsync(Symbol, cancellationToken);
    }

    public Task<DateOnly[]> GetOptionExpirationDatesAsync(YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        return _client.GetOptionExpirationDatesAsync(Symbol, cacheMode, cancellationToken);
    }

    public Task<OptionChainResult> GetOptionChainAsync(DateOnly? expirationDate = null, CancellationToken cancellationToken = default)
    {
        return _client.GetOptionChainAsync(Symbol, expirationDate, cancellationToken);
    }

    public Task<OptionChainResult> GetOptionChainAsync(DateOnly? expirationDate, YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        return _client.GetOptionChainAsync(Symbol, expirationDate, cacheMode, cancellationToken);
    }

    public Task<AnalystInsights> GetAnalystInsightsAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetAnalystInsightsAsync(Symbol, cancellationToken);
    }

    public Task<EarningsDatesResult> GetEarningsDatesAsync(int limit = 12, int offset = 0, CancellationToken cancellationToken = default)
    {
        return _client.GetEarningsDatesAsync(Symbol, limit, offset, cancellationToken);
    }

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

    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }
}