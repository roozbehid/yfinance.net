using System.Buffers;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

/// <summary>
/// Primary entry point for querying Yahoo Finance data from .NET.
/// </summary>
public sealed class YahooFinanceClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly YahooFinanceClientOptions _options;
    private readonly bool _ownsHttpClient;
    private readonly SemaphoreSlim _crumbLock = new(1, 1);
    private string? _crumb;

    /// <summary>
    /// Initializes a new client instance.
    /// </summary>
    /// <param name="httpClient">
    /// Optional <see cref="HttpClient"/> to reuse for requests. When omitted, the client creates and owns its own instance.
    /// </param>
    /// <param name="options">Optional client configuration such as headers, base URI, and cache settings.</param>
    public YahooFinanceClient(HttpClient? httpClient = null, YahooFinanceClientOptions? options = null)
    {
        _options = options ?? new YahooFinanceClientOptions();
        _httpClient = httpClient ?? CreateDefaultHttpClient();
        _ownsHttpClient = httpClient is null;

        ConfigureDefaultHeaders(_httpClient, _options);
    }

    /// <summary>
    /// Searches Yahoo Finance for matching quotes and related news.
    /// </summary>
    /// <param name="request">Search parameters such as the query text and result counts.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed Yahoo Finance search response.</returns>
    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        var payload = await SendGetBytesAsync(
            BuildSearchUri(request),
            "Yahoo Finance search endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return SearchResponseParser.Parse(payload);
    }

    /// <summary>
    /// Queries Yahoo Finance lookup results for a text query.
    /// </summary>
    /// <param name="request">Lookup parameters including query, paging, and category selection.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed lookup response.</returns>
    public async Task<LookupResult> LookupAsync(LookupRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);
        ArgumentOutOfRangeException.ThrowIfNegative(request.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(request.Start);

        var payload = await SendGetBytesAsync(
            BuildLookupUri(request),
            "Yahoo Finance lookup endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return LookupResponseParser.Parse(payload);
    }

    /// <inheritdoc cref="GetPredefinedScreenerAsync(string, PredefinedScreenerOptions?, CancellationToken)"/>
    public Task<ScreenerResult> GetPredefinedScreenerAsync(PredefinedScreenerId screenId, CancellationToken cancellationToken = default)
    {
        return GetPredefinedScreenerAsync(screenId, options: null, cancellationToken);
    }

    /// <inheritdoc cref="GetPredefinedScreenerAsync(string, PredefinedScreenerOptions?, CancellationToken)"/>
    public Task<ScreenerResult> GetPredefinedScreenerAsync(PredefinedScreenerId screenId, PredefinedScreenerOptions? options, CancellationToken cancellationToken = default)
    {
        return GetPredefinedScreenerAsync(screenId.ToWireValue(), options, cancellationToken);
    }

    /// <inheritdoc cref="GetPredefinedScreenerAsync(string, PredefinedScreenerOptions?, CancellationToken)"/>
    public Task<ScreenerResult> GetPredefinedScreenerAsync(string screenId, CancellationToken cancellationToken = default)
    {
        return GetPredefinedScreenerAsync(screenId, options: null, cancellationToken);
    }

    /// <summary>
    /// Gets the results of one of Yahoo Finance's predefined screeners.
    /// </summary>
    /// <param name="screenId">Yahoo's screener identifier, such as <c>day_gainers</c>.</param>
    /// <param name="options">Optional paging and localization settings.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The screener result returned by Yahoo Finance.</returns>
    public async Task<ScreenerResult> GetPredefinedScreenerAsync(string screenId, PredefinedScreenerOptions? options, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(screenId);

        options ??= new PredefinedScreenerOptions();
        ArgumentOutOfRangeException.ThrowIfNegative(options.Offset);

        if (options.Count is < 1 or > 250)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Count, "Yahoo predefined screener count must be between 1 and 250.");
        }

        var payload = await SendGetBytesAsync(
            BuildPredefinedScreenerUri(screenId, options),
            "Yahoo Finance predefined screener endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return PredefinedScreenerResponseParser.Parse(payload);
    }

    /// <inheritdoc cref="ScreenAsync(ScreenerQuery, ScreenerOptions?, CancellationToken)"/>
    public Task<ScreenerResult> ScreenAsync(ScreenerQuery query, CancellationToken cancellationToken = default)
    {
        return ScreenAsync(query, options: null, cancellationToken);
    }

    /// <inheritdoc cref="ScreenAsync(ScreenerQuery, ScreenerOptions?, CancellationToken)"/>
    public Task<ScreenerResult> ScreenAsync(ScreenerDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return ScreenAsync(definition.Query, definition.Options, cancellationToken);
    }

    /// <summary>
    /// Executes a custom Yahoo Finance screener query.
    /// </summary>
    /// <param name="query">The filter expression to execute.</param>
    /// <param name="options">Optional paging, sorting, quote type, and localization settings.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The screener result returned by Yahoo Finance.</returns>
    public async Task<ScreenerResult> ScreenAsync(ScreenerQuery query, ScreenerOptions? options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        options ??= new ScreenerOptions();
        ArgumentOutOfRangeException.ThrowIfNegative(options.Offset);

        if (options.Count is < 1 or > 250)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Count, "Yahoo screener count must be between 1 and 250.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(options.SortField);

        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);
        var payload = await SendPostBytesAsync(
            BuildCustomScreenerUri(options, crumb),
            BuildCustomScreenerBody(query, options),
            "Yahoo Finance screener endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return PredefinedScreenerResponseParser.Parse(payload);
    }

    /// <summary>
    /// Gets basic quote summary data for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Basic market data and summary fields for the symbol.</returns>
    public async Task<QuoteSummary> GetQuoteSummaryAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var payload = await GetQuoteSummaryPayloadAsync(
            symbol,
            "price,quoteType,summaryDetail",
            cancellationToken).ConfigureAwait(false);

        return QuoteSummaryResponseParser.Parse(payload);
    }

    /// <summary>
    /// Gets company profile and key statistics for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>Company profile data assembled from Yahoo Finance quote summary modules.</returns>
    public async Task<CompanyProfile> GetCompanyProfileAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var payload = await GetQuoteSummaryPayloadAsync(
            symbol,
            "price,quoteType,summaryDetail,assetProfile,financialData,defaultKeyStatistics",
            cancellationToken).ConfigureAwait(false);

        return CompanyProfileResponseParser.Parse(payload);
    }

    /// <summary>
    /// Gets major holder, fund holder, and institutional holder information for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed holder snapshot.</returns>
    public async Task<HoldersSnapshot> GetHoldersAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var payload = await GetQuoteSummaryPayloadAsync(
            symbol,
            "institutionOwnership,fundOwnership,majorHoldersBreakdown",
            cancellationToken).ConfigureAwait(false);

        return HoldersResponseParser.Parse(payload, symbol);
    }

    /// <summary>
    /// Gets insider transactions, insider holders, and net share purchase activity for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed insider snapshot.</returns>
    public async Task<InsiderSnapshot> GetInsiderDataAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var payload = await GetQuoteSummaryPayloadAsync(
            symbol,
            "insiderTransactions,insiderHolders,netSharePurchaseActivity",
            cancellationToken).ConfigureAwait(false);

        return InsiderDataResponseParser.Parse(payload, symbol);
    }

    /// <summary>
    /// Gets the valuation measures table from Yahoo Finance's key statistics page.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed valuation measures table.</returns>
    public async Task<ValuationMeasures> GetValuationMeasuresAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        _ = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);

        var payload = await SendGetTextRequestAsync(
            BuildValuationMeasuresUri(symbol),
            "Yahoo Finance key-statistics page is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return ValuationMeasuresResponseParser.Parse(payload);
    }

    /// <summary>
    /// Attempts to resolve an ISIN for a ticker symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The ISIN when one can be resolved; otherwise <see langword="null"/>.</returns>
    public async Task<string?> GetIsinAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        // Python yfinance intentionally gives up early for symbols that do not map cleanly to the BusinessInsider lookup.
        if (symbol.Contains('-', StringComparison.Ordinal) || symbol.Contains('^', StringComparison.Ordinal))
        {
            return null;
        }

        var payload = await SendGetTextRequestAsync(
            BuildIsinLookupUri(symbol),
            "BusinessInsider ISIN lookup endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return IsinLookupResponseParser.ParseForSymbol(payload, symbol);
    }

    /// <summary>
    /// Searches Yahoo Finance by ISIN and returns the matched quote plus related news.
    /// </summary>
    /// <param name="isin">ISIN to search for.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The aggregated ISIN search result.</returns>
    public async Task<IsinSearchResult> GetByIsinAsync(string isin, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isin);
        if (!IsinLookupResponseParser.IsValidIsin(isin))
        {
            throw new ArgumentException("Invalid ISIN number.", nameof(isin));
        }

        var result = await SearchAsync(new SearchRequest
        {
            Query = isin,
            QuotesCount = 1,
            RecommendedCount = 1
        }, cancellationToken).ConfigureAwait(false);

        SearchQuote? ticker = result.Quotes.Length > 0 ? result.Quotes[0] : null;
        return new IsinSearchResult(isin, ticker, result.News);
    }

    /// <summary>
    /// Gets the first ticker symbol matched for an ISIN.
    /// </summary>
    /// <param name="isin">ISIN to search for.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The matched ticker symbol, or <see langword="null"/> when none is found.</returns>
    public async Task<string?> GetTickerByIsinAsync(string isin, CancellationToken cancellationToken = default)
    {
        return (await GetByIsinAsync(isin, cancellationToken).ConfigureAwait(false)).Ticker?.Symbol;
    }

    /// <summary>
    /// Gets the first quote matched for an ISIN.
    /// </summary>
    /// <param name="isin">ISIN to search for.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The matched quote, or <see langword="null"/> when none is found.</returns>
    public async Task<SearchQuote?> GetInfoByIsinAsync(string isin, CancellationToken cancellationToken = default)
    {
        return (await GetByIsinAsync(isin, cancellationToken).ConfigureAwait(false)).Ticker;
    }

    /// <summary>
    /// Gets news items returned when searching by ISIN.
    /// </summary>
    /// <param name="isin">ISIN to search for.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The news items returned by the ISIN search.</returns>
    public async Task<SearchNewsItem[]> GetNewsByIsinAsync(string isin, CancellationToken cancellationToken = default)
    {
        return (await GetByIsinAsync(isin, cancellationToken).ConfigureAwait(false)).News;
    }

    /// <summary>
    /// Gets an income statement for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="frequency">Financial statement frequency to request.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed income statement.</returns>
    public async Task<IncomeStatement> GetIncomeStatementAsync(
        string symbol,
        FinancialStatementFrequency frequency = FinancialStatementFrequency.Annual,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ValidateFinancialStatementFrequency(frequency, allowTrailing: true, nameof(frequency));

        var payload = await SendGetBytesAsync(
            BuildIncomeStatementUri(symbol, frequency),
            "Yahoo Finance fundamentals-timeseries endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return IncomeStatementResponseParser.Parse(payload, symbol, frequency);
    }

    /// <summary>
    /// Gets the trailing twelve month income statement for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed trailing income statement.</returns>
    public Task<IncomeStatement> GetTrailingIncomeStatementAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return GetIncomeStatementAsync(symbol, FinancialStatementFrequency.Trailing, cancellationToken);
    }

    /// <summary>
    /// Gets a balance sheet for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="frequency">Financial statement frequency to request.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed balance sheet.</returns>
    public async Task<BalanceSheet> GetBalanceSheetAsync(
        string symbol,
        FinancialStatementFrequency frequency = FinancialStatementFrequency.Annual,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ValidateFinancialStatementFrequency(frequency, allowTrailing: false, nameof(frequency));

        var payload = await SendGetBytesAsync(
            BuildBalanceSheetUri(symbol, frequency),
            "Yahoo Finance fundamentals-timeseries endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return BalanceSheetResponseParser.Parse(payload, symbol, frequency);
    }

    /// <summary>
    /// Gets a cash flow statement for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="frequency">Financial statement frequency to request.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed cash flow statement.</returns>
    public async Task<CashFlow> GetCashFlowAsync(
        string symbol,
        FinancialStatementFrequency frequency = FinancialStatementFrequency.Annual,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ValidateFinancialStatementFrequency(frequency, allowTrailing: false, nameof(frequency));

        var payload = await SendGetBytesAsync(
            BuildCashFlowUri(symbol, frequency),
            "Yahoo Finance fundamentals-timeseries endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return CashFlowResponseParser.Parse(payload, symbol, frequency);
    }

    /// <summary>
    /// Gets ticker-specific news from Yahoo Finance.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="count">Maximum number of news items to request.</param>
    /// <param name="tab">News tab to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed list of news items.</returns>
    public async Task<TickerNewsItem[]> GetNewsAsync(string symbol, int count = 10, TickerNewsTab tab = TickerNewsTab.News, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ValidateTickerNewsRequest(count);

        var payload = await SendPostBytesAsync(
            BuildTickerNewsUri(tab),
            BuildTickerNewsBody(symbol, count),
            "Yahoo Finance ticker news endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return TickerNewsResponseParser.Parse(payload);
    }

    /// <inheritdoc cref="GetOptionExpirationDatesAsync(string, YFinanceCacheMode, CancellationToken)"/>
    public async Task<DateOnly[]> GetOptionExpirationDatesAsync(string symbol, CancellationToken cancellationToken = default)
        => await GetOptionExpirationDatesAsync(symbol, YFinanceCacheMode.Default, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets available option expiration dates for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="cacheMode">Controls whether the option expiration cache is used or refreshed.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The available option expiration dates.</returns>
    public async Task<DateOnly[]> GetOptionExpirationDatesAsync(string symbol, YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (TryGetCachedOptionExpirations(symbol, cacheMode, out var cachedExpirations))
        {
            return cachedExpirations;
        }

        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);

        var payload = await SendGetBytesAsync(
            BuildOptionsUri(symbol, expirationDate: null, crumb),
            "Yahoo Finance options endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        var expirations = OptionsResponseParser.Parse(payload, symbol).ExpirationDates;
        CacheOptionExpirations(symbol, expirations, cacheMode);
        return expirations;
    }

    /// <inheritdoc cref="GetOptionChainAsync(string, DateOnly?, YFinanceCacheMode, CancellationToken)"/>
    public async Task<OptionChainResult> GetOptionChainAsync(string symbol, DateOnly? expirationDate = null, CancellationToken cancellationToken = default)
        => await GetOptionChainAsync(symbol, expirationDate, YFinanceCacheMode.Default, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the option chain for a symbol, optionally limited to a specific expiration date.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="expirationDate">Optional expiration date to request. When omitted, Yahoo's default option chain payload is returned.</param>
    /// <param name="cacheMode">Controls whether the expiration cache is used when validating the requested expiration.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed option chain result.</returns>
    public async Task<OptionChainResult> GetOptionChainAsync(string symbol, DateOnly? expirationDate, YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);

        if (expirationDate is not null)
        {
            var expirations = await GetOptionExpirationDatesAsync(symbol, cacheMode, cancellationToken).ConfigureAwait(false);
            if (!expirations.Contains(expirationDate.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expirationDate),
                    expirationDate,
                    $"Expiration `{expirationDate:yyyy-MM-dd}` cannot be found. Available expirations are: [{string.Join(", ", expirations.Select(date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))}]");
            }
        }

        try
        {
            var payload = await SendGetBytesAsync(
                BuildOptionsUri(symbol, expirationDate, crumb),
                "Yahoo Finance options endpoint is temporarily unavailable.",
                cancellationToken).ConfigureAwait(false);

            var result = OptionsResponseParser.Parse(payload, symbol);
            CacheOptionExpirations(symbol, result.ExpirationDates, cacheMode);
            return result;
        }
        catch (InvalidOperationException) when (expirationDate is not null)
        {
            var availableExpirations = await GetOptionExpirationDatesAsync(symbol, cacheMode, cancellationToken).ConfigureAwait(false);
            if (!availableExpirations.Contains(expirationDate.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expirationDate),
                    expirationDate,
                    $"Expiration `{expirationDate:yyyy-MM-dd}` cannot be found. Available expirations are: [{string.Join(", ", availableExpirations.Select(date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))}]");
            }

            throw;
        }
    }

    /// <summary>
    /// Gets analyst trends, recommendation trends, and related insights for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed analyst insights payload.</returns>
    public async Task<AnalystInsights> GetAnalystInsightsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var payload = await GetQuoteSummaryPayloadAsync(
            symbol,
            "price,financialData,recommendationTrend,upgradeDowngradeHistory,earningsHistory,earningsTrend,industryTrend,sectorTrend,indexTrend",
            cancellationToken).ConfigureAwait(false);

        return AnalystInsightsResponseParser.Parse(payload);
    }

    /// <summary>
    /// Gets market summary data for a Yahoo Finance market region.
    /// </summary>
    /// <param name="region">Market region to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed market summary.</returns>
    public async Task<MarketSummaryResult> GetMarketSummaryAsync(MarketRegion region, CancellationToken cancellationToken = default)
    {
        var payload = await SendGetBytesAsync(
            BuildMarketSummaryUri(region),
            "Yahoo Finance market summary endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return MarketResponseParser.ParseSummary(payload, region);
    }

    /// <summary>
    /// Gets the current market status for a Yahoo Finance market region.
    /// </summary>
    /// <param name="region">Market region to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The current market status when Yahoo returns one; otherwise <see langword="null"/>.</returns>
    public async Task<MarketStatus?> GetMarketStatusAsync(MarketRegion region, CancellationToken cancellationToken = default)
    {
        var payload = await SendGetBytesAsync(
            BuildMarketStatusUri(region),
            "Yahoo Finance market time endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return MarketResponseParser.ParseStatus(payload, region);
    }

    /// <inheritdoc cref="GetSectorAsync(string, string, YFinanceCacheMode, CancellationToken)"/>
    public async Task<SectorDetails> GetSectorAsync(string key, string region = "US", CancellationToken cancellationToken = default)
        => await GetSectorAsync(key, region, YFinanceCacheMode.Default, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets sector details for a Yahoo Finance sector key.
    /// </summary>
    /// <param name="key">Sector key such as <c>technology</c>.</param>
    /// <param name="region">Region code used by Yahoo Finance, such as <c>US</c>.</param>
    /// <param name="cacheMode">Controls whether the domain cache is used or refreshed.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed sector details.</returns>
    public async Task<SectorDetails> GetSectorAsync(string key, string region, YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var normalizedRegion = Sector.NormalizeRegion(region);

        if (TryGetCachedSectorDetails(key, normalizedRegion, cacheMode, out var cachedSector))
        {
            return cachedSector;
        }

        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);

        var payload = await SendGetBytesAsync(
            BuildSectorUri(key, normalizedRegion, crumb),
            "Yahoo Finance sector endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        var result = DomainResponseParser.ParseSector(payload, key, normalizedRegion);
        CacheSectorDetails(key, normalizedRegion, result, cacheMode);
        return result;
    }

    /// <inheritdoc cref="GetIndustryAsync(string, string, YFinanceCacheMode, CancellationToken)"/>
    public async Task<IndustryDetails> GetIndustryAsync(string key, string region = "US", CancellationToken cancellationToken = default)
        => await GetIndustryAsync(key, region, YFinanceCacheMode.Default, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets industry details for a Yahoo Finance industry key.
    /// </summary>
    /// <param name="key">Industry key such as <c>software-infrastructure</c>.</param>
    /// <param name="region">Region code used by Yahoo Finance, such as <c>US</c>.</param>
    /// <param name="cacheMode">Controls whether the domain cache is used or refreshed.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed industry details.</returns>
    public async Task<IndustryDetails> GetIndustryAsync(string key, string region, YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var normalizedRegion = Sector.NormalizeRegion(region);

        if (TryGetCachedIndustryDetails(key, normalizedRegion, cacheMode, out var cachedIndustry))
        {
            return cachedIndustry;
        }

        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);

        var payload = await SendGetBytesAsync(
            BuildIndustryUri(key, normalizedRegion, crumb),
            "Yahoo Finance industry endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        var result = DomainResponseParser.ParseIndustry(payload, key, normalizedRegion);
        CacheIndustryDetails(key, normalizedRegion, result, cacheMode);
        return result;
    }

    /// <summary>
    /// Gets Yahoo Finance's earnings calendar.
    /// </summary>
    /// <param name="request">Optional calendar filters, paging, and date range settings.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed earnings calendar result.</returns>
    public async Task<CalendarResult<EarningsCalendarEntry>> GetEarningsCalendarAsync(EarningsCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new EarningsCalendarRequest();
        ValidateCalendarRequest(request.Limit, request.Offset);
        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);
        var dateRange = ResolveCalendarDateRange(request.Start, request.End);

        var operands = new List<object>
        {
            Query("eq", "region", "us"),
            Query("or", Query("eq", "eventtype", "EAD"), Query("eq", "eventtype", "ERA")),
            Query("gte", "startdatetime", FormatCalendarDate(dateRange.Start)),
            Query("lte", "startdatetime", FormatCalendarDate(dateRange.End))
        };

        if (request.MinimumMarketCap is not null)
        {
            operands.Add(Query("gte", "intradaymarketcap", request.MinimumMarketCap.Value));
        }

        var payload = await SendPostRequestAsync(
            BuildCalendarUri(crumb),
            BuildCalendarBody(
                entityIdType: "sp_earnings",
                sortField: "intradaymarketcap",
                includeFields: ["ticker", "companyshortname", "intradaymarketcap", "eventname", "startdatetime", "startdatetimetype", "epsestimate", "epsactual", "epssurprisepct"],
                limit: request.Limit,
                offset: request.Offset,
                query: Query("and", operands.ToArray())),
            "Yahoo Finance visualization endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return CalendarResponseParser.ParseEarnings(payload);
    }

    /// <summary>
    /// Gets Yahoo Finance's IPO calendar.
    /// </summary>
    /// <param name="request">Optional calendar filters, paging, and date range settings.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed IPO calendar result.</returns>
    public async Task<CalendarResult<IpoCalendarEntry>> GetIpoCalendarAsync(IpoCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new IpoCalendarRequest();
        ValidateCalendarRequest(request.Limit, request.Offset);
        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);
        var dateRange = ResolveCalendarDateRange(request.Start, request.End);

        var payload = await SendPostRequestAsync(
            BuildCalendarUri(crumb),
            BuildCalendarBody(
                entityIdType: "ipo_info",
                sortField: "startdatetime",
                includeFields: ["ticker", "companyshortname", "exchange_short_name", "filingdate", "startdatetime", "amendeddate", "pricefrom", "priceto", "offerprice", "currencyname", "shares", "dealtype"],
                limit: request.Limit,
                offset: request.Offset,
                query: Query(
                    "or",
                    Query("gtelt", "startdatetime", FormatCalendarDate(dateRange.Start), FormatCalendarDate(dateRange.End)),
                    Query("gtelt", "filingdate", FormatCalendarDate(dateRange.Start), FormatCalendarDate(dateRange.End)),
                    Query("gtelt", "amendeddate", FormatCalendarDate(dateRange.Start), FormatCalendarDate(dateRange.End)))),
            "Yahoo Finance visualization endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return CalendarResponseParser.ParseIpos(payload);
    }

    /// <summary>
    /// Gets Yahoo Finance's economic events calendar.
    /// </summary>
    /// <param name="request">Optional calendar filters, paging, and date range settings.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed economic events calendar result.</returns>
    public async Task<CalendarResult<EconomicEventCalendarEntry>> GetEconomicEventsCalendarAsync(EconomicEventCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new EconomicEventCalendarRequest();
        ValidateCalendarRequest(request.Limit, request.Offset);
        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);
        var dateRange = ResolveCalendarDateRange(request.Start, request.End);

        var payload = await SendPostRequestAsync(
            BuildCalendarUri(crumb),
            BuildCalendarBody(
                entityIdType: "economic_event",
                sortField: "startdatetime",
                includeFields: ["econ_release", "country_code", "startdatetime", "period", "after_release_actual", "consensus_estimate", "prior_release_actual", "originally_reported_actual"],
                limit: request.Limit,
                offset: request.Offset,
                query: BuildStartDateQuery(dateRange)),
            "Yahoo Finance visualization endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return CalendarResponseParser.ParseEconomicEvents(payload);
    }

    /// <summary>
    /// Gets Yahoo Finance's stock split calendar.
    /// </summary>
    /// <param name="request">Optional calendar filters, paging, and date range settings.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed stock split calendar result.</returns>
    public async Task<CalendarResult<SplitCalendarEntry>> GetSplitsCalendarAsync(SplitsCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new SplitsCalendarRequest();
        ValidateCalendarRequest(request.Limit, request.Offset);
        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);
        var dateRange = ResolveCalendarDateRange(request.Start, request.End);

        var payload = await SendPostRequestAsync(
            BuildCalendarUri(crumb),
            BuildCalendarBody(
                entityIdType: "splits",
                sortField: "startdatetime",
                includeFields: ["ticker", "companyshortname", "startdatetime", "optionable", "old_share_worth", "share_worth"],
                limit: request.Limit,
                offset: request.Offset,
                query: BuildStartDateQuery(dateRange)),
            "Yahoo Finance visualization endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return CalendarResponseParser.ParseSplits(payload);
    }

    /// <summary>
    /// Gets historical or upcoming earnings dates for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol to query.</param>
    /// <param name="limit">Maximum number of rows to return.</param>
    /// <param name="offset">Result offset for paging.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed earnings dates result.</returns>
    public async Task<EarningsDatesResult> GetEarningsDatesAsync(string symbol, int limit = 12, int offset = 0, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ValidateEarningsDatesRequest(limit, offset);
        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);

        var payload = await SendPostBytesAsync(
            BuildCalendarUri(crumb),
            BuildCalendarBody(
                entityIdType: "earnings",
                sortField: "startdatetime",
                includeFields: ["startdatetime", "timeZoneShortName", "epsestimate", "epsactual", "epssurprisepct", "eventtype"],
                limit: limit,
                offset: offset,
                query: Query("eq", "ticker", symbol)),
            "Yahoo Finance visualization endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return EarningsDatesResponseParser.Parse(payload);
    }

    /// <summary>
    /// Gets company profiles for multiple symbols with bounded concurrency.
    /// </summary>
    /// <param name="request">Batch request describing symbols and concurrency.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The batch company profile result, including per-symbol failures.</returns>
    public async Task<BatchCompanyProfileResult> GetCompanyProfilesAsync(BatchCompanyProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBatchCompanyProfileRequest(request);

        var distinctSymbols = request.Symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var (profiles, failures) = await ExecuteBoundedBatchAsync(
            distinctSymbols,
            request.MaxConcurrency,
            static (symbol, state, token) => state.Client.GetCompanyProfileAsync(symbol, token),
            static (symbol, ex) => new BatchCompanyProfileFailure(symbol, ex.Message),
            new BatchExecutionState(this),
            cancellationToken).ConfigureAwait(false);

        return new BatchCompanyProfileResult(profiles, failures);
    }

    /// <summary>
    /// Gets ticker news for multiple symbols with bounded concurrency.
    /// </summary>
    /// <param name="request">Batch request describing symbols, count, tab, and concurrency.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The batch ticker news result, including per-symbol failures.</returns>
    public async Task<BatchTickerNewsResult> GetNewsAsync(BatchTickerNewsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBatchTickerNewsRequest(request);

        var distinctSymbols = request.Symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var (newsBySymbol, failures) = await ExecuteBoundedBatchAsync(
            distinctSymbols,
            request.MaxConcurrency,
            static (symbol, state, token) => state.Client.GetNewsAsync(symbol, state.Request.Count, state.Request.Tab, token),
            static (symbol, ex) => new BatchTickerNewsFailure(symbol, ex.Message),
            new BatchTickerNewsExecutionState(this, request),
            cancellationToken).ConfigureAwait(false);

        return new BatchTickerNewsResult(newsBySymbol, failures);
    }

    /// <summary>
    /// Gets historical price data for a symbol.
    /// </summary>
    /// <param name="request">Price history request specifying symbol, date range, interval, and shaping options.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The parsed and post-processed price history result.</returns>
    public async Task<PriceHistoryResult> GetPriceHistoryAsync(PriceHistoryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Symbol);
        ValidatePriceHistoryRequest(request);

        var payload = await SendGetBytesAsync(
            BuildPriceHistoryUri(request),
            "Yahoo Finance chart endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        var history = PriceHistoryResponseParser.Parse(payload);
        history = PriceHistoryAdjuster.Apply(history, request.AdjustmentMode);
        history = PriceHistoryTimestampShaper.Apply(history, request.TimestampMode);
        return history;
    }

    /// <summary>
    /// Gets historical price data for multiple symbols with bounded concurrency.
    /// </summary>
    /// <param name="request">Batch price history request describing symbols, history settings, and concurrency.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The batch price history result, including per-symbol failures.</returns>
    public async Task<BatchPriceHistoryResult> GetPriceHistoriesAsync(BatchPriceHistoryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBatchPriceHistoryRequest(request);

        var distinctSymbols = request.Symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var (histories, failures) = await ExecuteBoundedBatchAsync(
            distinctSymbols,
            request.MaxConcurrency,
            static (symbol, state, token) => state.Client.GetPriceHistoryAsyncCore(symbol, state.Request, token),
            static (symbol, ex) => new BatchHistoryFailure(symbol, ex.Message),
            new BatchPriceHistoryExecutionState(this, request),
            cancellationToken).ConfigureAwait(false);

        return new BatchPriceHistoryResult(histories, failures);
    }

    /// <summary>
    /// Disposes the client and its owned <see cref="HttpClient"/> instance when applicable.
    /// </summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<PriceHistoryResult> GetPriceHistoryAsyncCore(string symbol, BatchPriceHistoryRequest request, CancellationToken cancellationToken)
    {
        var payload = await SendGetBytesAsync(
            BuildPriceHistoryUri(symbol, request.Range, request.Interval, request.Start, request.End, request.IncludePrePost),
            "Yahoo Finance chart endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);

        return TransformHistory(PriceHistoryResponseParser.Parse(payload), request.AdjustmentMode, request.TimestampMode);
    }

    private static PriceHistoryResult TransformHistory(PriceHistoryResult history, PriceAdjustmentMode adjustmentMode, PriceTimestampMode timestampMode)
    {
        history = PriceHistoryAdjuster.Apply(history, adjustmentMode);
        history = PriceHistoryTimestampShaper.Apply(history, timestampMode);
        return history;
    }

    private static string[] NormalizeSymbols(IReadOnlyList<string> symbols)
    {
        return symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static async Task<(Dictionary<string, TResult> Successes, Dictionary<string, TFailure> Failures)> ExecuteBoundedBatchAsync<TResult, TFailure, TState>(
        IReadOnlyList<string> symbols,
        int maxConcurrency,
        Func<string, TState, CancellationToken, Task<TResult>> action,
        Func<string, Exception, TFailure> failureFactory,
        TState state,
        CancellationToken cancellationToken)
        where TResult : class
        where TFailure : class
    {
        var successSlots = new TResult?[symbols.Count];
        var failureSlots = new TFailure?[symbols.Count];
        var nextIndex = -1;
        var workerCount = Math.Min(maxConcurrency, symbols.Count);
        var workers = new Task[workerCount];

        for (var workerIndex = 0; workerIndex < workerCount; workerIndex++)
        {
            workers[workerIndex] = Task.Run(async () =>
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var index = Interlocked.Increment(ref nextIndex);
                    if (index >= symbols.Count)
                    {
                        return;
                    }

                    var symbol = symbols[index];
                    try
                    {
                        successSlots[index] = await action(symbol, state, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        failureSlots[index] = failureFactory(symbol, ex);
                    }
                }
            }, cancellationToken);
        }

        await Task.WhenAll(workers).ConfigureAwait(false);

        var successes = new Dictionary<string, TResult>(symbols.Count, StringComparer.OrdinalIgnoreCase);
        var failures = new Dictionary<string, TFailure>(symbols.Count, StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < symbols.Count; index++)
        {
            if (successSlots[index] is not null)
            {
                successes[symbols[index]] = successSlots[index]!;
            }
            else if (failureSlots[index] is not null)
            {
                failures[symbols[index]] = failureSlots[index]!;
            }
        }

        return (successes, failures);
    }

    private Uri BuildSearchUri(SearchRequest request)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["q"] = request.Query,
            ["quotesCount"] = request.QuotesCount.ToString(),
            ["enableFuzzyQuery"] = request.EnableFuzzyQuery.ToString().ToLowerInvariant(),
            ["newsCount"] = request.NewsCount.ToString(),
            ["quotesQueryId"] = "tss_match_phrase_query",
            ["newsQueryId"] = "news_cie_vespa",
            ["listsCount"] = request.ListsCount.ToString(),
            ["enableCb"] = request.IncludeCompanyBreakdown.ToString().ToLowerInvariant(),
            ["enableNavLinks"] = request.IncludeNavigationLinks.ToString().ToLowerInvariant(),
            ["enableResearchReports"] = request.IncludeResearchReports.ToString().ToLowerInvariant(),
            ["enableCulturalAssets"] = request.IncludeCulturalAssets.ToString().ToLowerInvariant(),
            ["recommendedCount"] = request.RecommendedCount.ToString()
        };

        return BuildUri(new Uri(_options.BaseUri, "/v1/finance/search"), queryParameters);
    }

    private static Uri BuildLookupUri(LookupRequest request)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["query"] = request.Query,
            ["type"] = LookupTypeToWireValue(request.Type),
            ["start"] = request.Start.ToString(),
            ["count"] = request.Count.ToString(),
            ["formatted"] = false.ToString().ToLowerInvariant(),
            ["fetchPricingData"] = request.FetchPricingData.ToString().ToLowerInvariant(),
            ["lang"] = request.Language,
            ["region"] = request.Region
        };

        return BuildUri(new Uri("https://query1.finance.yahoo.com/v1/finance/lookup", UriKind.Absolute), queryParameters);
    }

    private static Uri BuildPredefinedScreenerUri(string screenId, PredefinedScreenerOptions options)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["scrIds"] = screenId,
            ["count"] = options.Count.ToString(CultureInfo.InvariantCulture),
            ["offset"] = options.Offset.ToString(CultureInfo.InvariantCulture),
            ["formatted"] = false.ToString().ToLowerInvariant(),
            ["lang"] = options.Language,
            ["region"] = options.Region,
            ["corsDomain"] = "finance.yahoo.com"
        };

        return BuildUri(new Uri("https://query1.finance.yahoo.com/v1/finance/screener/predefined/saved", UriKind.Absolute), queryParameters);
    }

    private static Uri BuildCustomScreenerUri(ScreenerOptions options, string crumb)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["formatted"] = false.ToString().ToLowerInvariant(),
            ["lang"] = options.Language,
            ["region"] = options.Region,
            ["corsDomain"] = "finance.yahoo.com",
            ["crumb"] = crumb
        };

        return BuildUri(new Uri("https://query1.finance.yahoo.com/v1/finance/screener", UriKind.Absolute), queryParameters);
    }

    private Uri BuildQuoteSummaryUri(string symbol, string modules, string crumb)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["modules"] = modules,
            ["corsDomain"] = "finance.yahoo.com",
            ["formatted"] = false.ToString().ToLowerInvariant(),
            ["symbol"] = symbol,
            ["lang"] = "en-US",
            ["region"] = "US",
            ["crumb"] = crumb
        };

        return BuildUri(new Uri(_options.BaseUri, $"/v10/finance/quoteSummary/{Uri.EscapeDataString(symbol)}"), queryParameters);
    }

    private static Uri BuildIncomeStatementUri(string symbol, FinancialStatementFrequency frequency)
    {
        var prefix = frequency.ToWirePrefix();
        var types = string.Join(",", IncomeStatementKeys.All.Select(key => $"{prefix}{key}"));
        var queryParameters = new Dictionary<string, string?>
        {
            ["symbol"] = symbol,
            ["type"] = types,
            ["period1"] = DateTimeOffset.Parse("2016-12-31T00:00:00+00:00", CultureInfo.InvariantCulture).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            ["period2"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        };

        return BuildUri(new Uri($"https://query2.finance.yahoo.com/ws/fundamentals-timeseries/v1/finance/timeseries/{Uri.EscapeDataString(symbol)}", UriKind.Absolute), queryParameters);
    }

    private static Uri BuildBalanceSheetUri(string symbol, FinancialStatementFrequency frequency)
    {
        var prefix = frequency.ToWirePrefix();
        var types = string.Join(",", BalanceSheetKeys.All.Select(key => $"{prefix}{key}"));
        var queryParameters = new Dictionary<string, string?>
        {
            ["symbol"] = symbol,
            ["type"] = types,
            ["period1"] = DateTimeOffset.Parse("2016-12-31T00:00:00+00:00", CultureInfo.InvariantCulture).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            ["period2"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        };

        return BuildUri(new Uri($"https://query2.finance.yahoo.com/ws/fundamentals-timeseries/v1/finance/timeseries/{Uri.EscapeDataString(symbol)}", UriKind.Absolute), queryParameters);
    }

    private static Uri BuildValuationMeasuresUri(string symbol)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["p"] = symbol
        };

        return BuildUri(new Uri($"https://finance.yahoo.com/quote/{Uri.EscapeDataString(symbol)}/key-statistics", UriKind.Absolute), queryParameters);
    }

    private static Uri BuildIsinLookupUri(string query)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["max_results"] = "25",
            ["query"] = query
        };

        return BuildUri(new Uri("https://markets.businessinsider.com/ajax/SearchController_Suggest", UriKind.Absolute), queryParameters);
    }

    private static Uri BuildCashFlowUri(string symbol, FinancialStatementFrequency frequency)
    {
        var prefix = frequency.ToWirePrefix();
        var types = string.Join(",", CashFlowKeys.All.Select(key => $"{prefix}{key}"));
        var queryParameters = new Dictionary<string, string?>
        {
            ["symbol"] = symbol,
            ["type"] = types,
            ["period1"] = DateTimeOffset.Parse("2016-12-31T00:00:00+00:00", CultureInfo.InvariantCulture).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            ["period2"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        };

        return BuildUri(new Uri($"https://query2.finance.yahoo.com/ws/fundamentals-timeseries/v1/finance/timeseries/{Uri.EscapeDataString(symbol)}", UriKind.Absolute), queryParameters);
    }

    private static void ValidateFinancialStatementFrequency(FinancialStatementFrequency frequency, bool allowTrailing, string paramName)
    {
        if (allowTrailing)
        {
            if (frequency is FinancialStatementFrequency.Annual or FinancialStatementFrequency.Quarterly or FinancialStatementFrequency.Trailing)
            {
                return;
            }
        }
        else if (frequency is FinancialStatementFrequency.Annual or FinancialStatementFrequency.Quarterly)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(paramName, frequency, allowTrailing
            ? "Supported financial statement frequencies are Annual, Quarterly, and Trailing."
            : "Supported financial statement frequencies are Annual and Quarterly for this statement type.");
    }

    private static Uri BuildMarketSummaryUri(MarketRegion region)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["fields"] = "shortName,regularMarketPrice,regularMarketChange,regularMarketChangePercent",
            ["formatted"] = false.ToString().ToLowerInvariant(),
            ["lang"] = "en-US",
            ["market"] = region.ToString()
        };

        return BuildUri(new Uri("https://query1.finance.yahoo.com/v6/finance/quote/marketSummary", UriKind.Absolute), queryParameters);
    }

    private static Uri BuildMarketStatusUri(MarketRegion region)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["formatted"] = true.ToString().ToLowerInvariant(),
            ["key"] = "finance",
            ["lang"] = "en-US",
            ["market"] = region.ToString()
        };

        return BuildUri(new Uri("https://query1.finance.yahoo.com/v6/finance/markettime", UriKind.Absolute), queryParameters);
    }

    private static Uri BuildTickerNewsUri(TickerNewsTab tab)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["queryRef"] = TickerNewsTabToQueryRef(tab),
            ["serviceKey"] = "ncp_fin"
        };

        return BuildUri(new Uri("https://finance.yahoo.com/xhr/ncp", UriKind.Absolute), queryParameters);
    }

    private static Uri BuildSectorUri(string key, string region, string crumb)
    {
        return BuildDomainUri($"/v1/finance/sectors/{Uri.EscapeDataString(key)}", region, crumb);
    }

    private static Uri BuildIndustryUri(string key, string region, string crumb)
    {
        return BuildDomainUri($"/v1/finance/industries/{Uri.EscapeDataString(key)}", region, crumb);
    }

    private static Uri BuildDomainUri(string path, string region, string crumb)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["formatted"] = true.ToString().ToLowerInvariant(),
            ["withReturns"] = true.ToString().ToLowerInvariant(),
            ["lang"] = "en-US",
            ["region"] = region,
            ["crumb"] = crumb
        };

        return BuildUri(new Uri($"https://query1.finance.yahoo.com{path}", UriKind.Absolute), queryParameters);
    }

    private static Uri BuildCalendarUri(string crumb)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["lang"] = "en-US",
            ["region"] = "US",
            ["crumb"] = crumb
        };

        return BuildUri(new Uri("https://query1.finance.yahoo.com/v1/finance/visualization", UriKind.Absolute), queryParameters);
    }

    private static Dictionary<string, object?> BuildCalendarBody(string entityIdType, string sortField, string[] includeFields, int limit, int offset, Dictionary<string, object?> query)
    {
        return new Dictionary<string, object?>
        {
            ["sortType"] = "DESC",
            ["entityIdType"] = entityIdType,
            ["sortField"] = sortField,
            ["includeFields"] = includeFields,
            ["size"] = Math.Min(limit, 100),
            ["offset"] = offset,
            ["query"] = query
        };
    }

    private static Dictionary<string, object?> BuildTickerNewsBody(string symbol, int count)
    {
        return new Dictionary<string, object?>
        {
            ["serviceConfig"] = new Dictionary<string, object?>
            {
                ["snippetCount"] = count,
                ["s"] = new[] { symbol }
            }
        };
    }

    private static byte[] BuildCustomScreenerBody(ScreenerQuery query, ScreenerOptions options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WriteNumber("offset", options.Offset);
        writer.WriteNumber("size", options.Count);
        writer.WriteString("sortField", options.SortField);
        writer.WriteString("sortType", options.SortOrder.ToWireValue());
        writer.WriteString("quoteType", options.QuoteType.ToWireValue());
        writer.WritePropertyName("query");
        query.WriteTo(writer);
        writer.WriteEndObject();
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static Dictionary<string, object?> BuildStartDateQuery((DateOnly Start, DateOnly End) dateRange)
    {
        return Query(
            "and",
            Query("gte", "startdatetime", FormatCalendarDate(dateRange.Start)),
            Query("lte", "startdatetime", FormatCalendarDate(dateRange.End)));
    }

    private static Dictionary<string, object?> Query(string op, params object[] operands)
    {
        return new Dictionary<string, object?>
        {
            ["operator"] = op,
            ["operands"] = operands
        };
    }

    private static string FormatCalendarDate(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static (DateOnly Start, DateOnly End) ResolveCalendarDateRange(DateOnly? start, DateOnly? end)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedStart = start ?? today;
        var resolvedEnd = end ?? resolvedStart.AddDays(7);
        return (resolvedStart, resolvedEnd);
    }

    private Uri BuildPriceHistoryUri(PriceHistoryRequest request)
    {
        return BuildPriceHistoryUri(request.Symbol, request.Range, request.Interval, request.Start, request.End, request.IncludePrePost);
    }

    private Uri BuildPriceHistoryUri(string symbol, string? range, string interval, DateTimeOffset? start, DateTimeOffset? end, bool includePrePost)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["interval"] = interval,
            ["includePrePost"] = includePrePost.ToString().ToLowerInvariant(),
            ["events"] = "div,splits,capitalGains"
        };

        if (!string.IsNullOrWhiteSpace(range))
        {
            queryParameters["range"] = range;
        }
        else
        {
            queryParameters["period1"] = start!.Value.ToUnixTimeSeconds().ToString();
            queryParameters["period2"] = end!.Value.ToUnixTimeSeconds().ToString();
        }

        return BuildUri(new Uri(_options.BaseUri, $"/v8/finance/chart/{Uri.EscapeDataString(symbol)}"), queryParameters);
    }

    private Uri BuildOptionsUri(string symbol, DateOnly? expirationDate, string crumb)
    {
        var queryParameters = new Dictionary<string, string?>
        {
            ["crumb"] = crumb
        };

        if (expirationDate is not null)
        {
            queryParameters["date"] = new DateTimeOffset(expirationDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        }

        return BuildUri(new Uri(_options.BaseUri, $"/v7/finance/options/{Uri.EscapeDataString(symbol)}"), queryParameters);
    }

    private bool TryGetCachedOptionExpirations(string symbol, YFinanceCacheMode cacheMode, out DateOnly[] expirations)
    {
        expirations = [];

        if (!ShouldReadOptionExpirationCache(cacheMode))
        {
            return false;
        }

        var cacheStore = _options.Cache.Store;
        if (cacheStore is null)
        {
            return false;
        }

        if (!cacheStore.TryGetValue<DateOnly[]>(BuildOptionExpirationCacheKey(symbol), out var cached) || cached is not { Length: > 0 })
        {
            return false;
        }

        expirations = (DateOnly[])cached.Clone();
        return true;
    }

    private void CacheOptionExpirations(string symbol, DateOnly[] expirations, YFinanceCacheMode cacheMode)
    {
        if (!ShouldWriteOptionExpirationCache(cacheMode) || expirations.Length == 0)
        {
            return;
        }

        _options.Cache.Store?.Set(BuildOptionExpirationCacheKey(symbol), (DateOnly[])expirations.Clone(), _options.Cache.OptionExpirationCacheTtl);
    }

    private bool TryGetCachedSectorDetails(string key, string region, YFinanceCacheMode cacheMode, out SectorDetails result)
    {
        result = default!;
        if (!ShouldReadCache(cacheMode, _options.Cache.EnableDomainCache) || _options.Cache.Store is null)
        {
            return false;
        }

        if (!_options.Cache.Store.TryGetValue<SectorDetails>(BuildSectorCacheKey(key, region), out var cached) || cached is null)
        {
            return false;
        }

        result = CloneSectorDetails(cached);
        return true;
    }

    private void CacheSectorDetails(string key, string region, SectorDetails value, YFinanceCacheMode cacheMode)
    {
        if (!ShouldWriteCache(cacheMode, _options.Cache.EnableDomainCache) || _options.Cache.Store is null)
        {
            return;
        }

        _options.Cache.Store.Set(BuildSectorCacheKey(key, region), CloneSectorDetails(value), _options.Cache.DomainCacheTtl);
    }

    private bool TryGetCachedIndustryDetails(string key, string region, YFinanceCacheMode cacheMode, out IndustryDetails result)
    {
        result = default!;
        if (!ShouldReadCache(cacheMode, _options.Cache.EnableDomainCache) || _options.Cache.Store is null)
        {
            return false;
        }

        if (!_options.Cache.Store.TryGetValue<IndustryDetails>(BuildIndustryCacheKey(key, region), out var cached) || cached is null)
        {
            return false;
        }

        result = CloneIndustryDetails(cached);
        return true;
    }

    private void CacheIndustryDetails(string key, string region, IndustryDetails value, YFinanceCacheMode cacheMode)
    {
        if (!ShouldWriteCache(cacheMode, _options.Cache.EnableDomainCache) || _options.Cache.Store is null)
        {
            return;
        }

        _options.Cache.Store.Set(BuildIndustryCacheKey(key, region), CloneIndustryDetails(value), _options.Cache.DomainCacheTtl);
    }

    private bool ShouldReadOptionExpirationCache(YFinanceCacheMode cacheMode)
    {
        return ShouldReadCache(cacheMode, _options.Cache.EnableOptionExpirationCache);
    }

    private bool ShouldWriteOptionExpirationCache(YFinanceCacheMode cacheMode)
    {
        return ShouldWriteCache(cacheMode, _options.Cache.EnableOptionExpirationCache);
    }

    private bool ShouldReadCache(YFinanceCacheMode cacheMode, bool featureEnabled)
    {
        if (!featureEnabled || _options.Cache.Store is null)
        {
            return false;
        }

        return ResolveCacheMode(cacheMode) switch
        {
            YFinanceCacheMode.UseCache => true,
            YFinanceCacheMode.Refresh => false,
            YFinanceCacheMode.BypassCache => false,
            _ => false
        };
    }

    private bool ShouldWriteCache(YFinanceCacheMode cacheMode, bool featureEnabled)
    {
        if (!featureEnabled || _options.Cache.Store is null)
        {
            return false;
        }

        return ResolveCacheMode(cacheMode) switch
        {
            YFinanceCacheMode.UseCache => true,
            YFinanceCacheMode.Refresh => true,
            YFinanceCacheMode.BypassCache => false,
            _ => false
        };
    }

    private YFinanceCacheMode ResolveCacheMode(YFinanceCacheMode cacheMode)
    {
        if (cacheMode != YFinanceCacheMode.Default)
        {
            return cacheMode;
        }

        return _options.Cache.DefaultMode == YFinanceCacheMode.Default
            ? YFinanceCacheMode.UseCache
            : _options.Cache.DefaultMode;
    }

    private static string BuildOptionExpirationCacheKey(string symbol)
    {
        return $"options-expirations:{symbol.Trim().ToUpperInvariant()}";
    }

    private static string BuildSectorCacheKey(string key, string region)
    {
        return $"domain:sector:{region.Trim().ToUpperInvariant()}:{key.Trim().ToLowerInvariant()}";
    }

    private static string BuildIndustryCacheKey(string key, string region)
    {
        return $"domain:industry:{region.Trim().ToUpperInvariant()}:{key.Trim().ToLowerInvariant()}";
    }

    private static SectorDetails CloneSectorDetails(SectorDetails value)
    {
        return value with
        {
            TopCompanies = (DomainCompany[])value.TopCompanies.Clone(),
            ResearchReports = (DomainResearchReport[])value.ResearchReports.Clone(),
            TopEtfs = (SymbolReference[])value.TopEtfs.Clone(),
            TopMutualFunds = (SymbolReference[])value.TopMutualFunds.Clone(),
            Industries = (SectorIndustryReference[])value.Industries.Clone()
        };
    }

    private static IndustryDetails CloneIndustryDetails(IndustryDetails value)
    {
        return value with
        {
            TopCompanies = (DomainCompany[])value.TopCompanies.Clone(),
            ResearchReports = (DomainResearchReport[])value.ResearchReports.Clone(),
            TopPerformingCompanies = (DomainCompany[])value.TopPerformingCompanies.Clone(),
            TopGrowthCompanies = (DomainCompany[])value.TopGrowthCompanies.Clone()
        };
    }

    private static Uri BuildUri(Uri baseUri, IReadOnlyDictionary<string, string?> queryParameters)
    {
        var builder = new UriBuilder(baseUri)
        {
            Query = string.Join("&", queryParameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value ?? string.Empty)}"))
        };

        return builder.Uri;
    }

    private async Task<string> GetCrumbAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_crumb))
        {
            return _crumb;
        }

        await _crumbLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrWhiteSpace(_crumb))
            {
                return _crumb;
            }

            using (var cookieRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("https://fc.yahoo.com", UriKind.Absolute)))
            {
                cookieRequest.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                using var cookieResponse = await _httpClient.SendAsync(cookieRequest, cancellationToken).ConfigureAwait(false);
            }

            using var crumbRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("https://query1.finance.yahoo.com/v1/test/getcrumb", UriKind.Absolute));
            crumbRequest.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            using var crumbResponse = await _httpClient.SendAsync(crumbRequest, cancellationToken).ConfigureAwait(false);
            crumbResponse.EnsureSuccessStatusCode();

            var crumb = (await crumbResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Trim();
            if (string.IsNullOrWhiteSpace(crumb) || crumb.Contains("<html>", StringComparison.OrdinalIgnoreCase) || crumb.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Yahoo Finance crumb acquisition failed.");
            }

            _crumb = crumb;
            return _crumb;
        }
        finally
        {
            _crumbLock.Release();
        }
    }

    private async Task<string> GetQuoteSummaryPayloadAsync(string symbol, string modules, CancellationToken cancellationToken)
    {
        var crumb = await GetCrumbAsync(cancellationToken).ConfigureAwait(false);

        return await SendGetRequestAsync(
            BuildQuoteSummaryUri(symbol, modules, crumb),
            "Yahoo Finance quoteSummary endpoint is temporarily unavailable.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> SendGetRequestAsync(Uri requestUri, string unavailableMessage, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (payload.Contains("Will be right back", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(unavailableMessage);
        }

        return payload;
    }

    private async Task<string> SendGetTextRequestAsync(Uri requestUri, string unavailableMessage, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!payload.Contains("<table", StringComparison.OrdinalIgnoreCase) && payload.Contains("Will be right back", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(unavailableMessage);
        }

        return payload;
    }

    private async Task<byte[]> SendGetBytesAsync(Uri requestUri, string unavailableMessage, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (payload.AsSpan().IndexOf("Will be right back"u8) >= 0)
        {
            throw new InvalidOperationException(unavailableMessage);
        }

        return payload;
    }

    private async Task<string> SendPostRequestAsync(Uri requestUri, object requestBody, string unavailableMessage, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (payload.Contains("Will be right back", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(unavailableMessage);
        }

        return payload;
    }

    private async Task<byte[]> SendPostBytesAsync(Uri requestUri, object requestBody, string unavailableMessage, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(requestBody));
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = Encoding.UTF8.WebName
        };
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (payload.AsSpan().IndexOf("Will be right back"u8) >= 0)
        {
            throw new InvalidOperationException(unavailableMessage);
        }

        return payload;
    }

    private async Task<byte[]> SendPostBytesAsync(Uri requestUri, byte[] requestBody, string unavailableMessage, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new ByteArrayContent(requestBody);
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = Encoding.UTF8.WebName
        };
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (payload.AsSpan().IndexOf("Will be right back"u8) >= 0)
        {
            throw new InvalidOperationException(unavailableMessage);
        }

        return payload;
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.All
        };

        return new HttpClient(handler);
    }

    private static string LookupTypeToWireValue(LookupType type)
    {
        return type switch
        {
            LookupType.All => "all",
            LookupType.Equity => "equity",
            LookupType.MutualFund => "mutualfund",
            LookupType.Etf => "etf",
            LookupType.Index => "index",
            LookupType.Future => "future",
            LookupType.Currency => "currency",
            LookupType.Cryptocurrency => "cryptocurrency",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static string TickerNewsTabToQueryRef(TickerNewsTab tab)
    {
        return tab switch
        {
            TickerNewsTab.All => "newsAll",
            TickerNewsTab.News => "latestNews",
            TickerNewsTab.PressReleases => "pressRelease",
            _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, null)
        };
    }

    private static void ValidatePriceHistoryRequest(PriceHistoryRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Interval);

        var usesRange = !string.IsNullOrWhiteSpace(request.Range);
        var usesExplicitDates = request.Start is not null || request.End is not null;

        if (usesRange && usesExplicitDates)
        {
            throw new ArgumentException("Set either Range or Start/End for price history, not both.");
        }

        if (!usesRange && (request.Start is null || request.End is null))
        {
            throw new ArgumentException("When Range is not set, both Start and End must be provided for price history.");
        }

        if (request.Start is not null && request.End is not null && request.Start >= request.End)
        {
            throw new ArgumentException("Price history Start must be earlier than End.");
        }
    }

    private static void ValidateCalendarRequest(int limit, int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
    }

    private static void ValidateEarningsDatesRequest(int limit, int offset)
    {
        if (limit <= 0 || limit > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Earnings dates limit must be between 1 and 100.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(offset);
    }

    private static void ValidateTickerNewsRequest(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Ticker news count must be greater than zero.");
        }
    }

    private static void ValidateBatchPriceHistoryRequest(BatchPriceHistoryRequest request)
    {
        if (request.Symbols is null || request.Symbols.Count == 0)
        {
            throw new ArgumentException("Batch price history requires at least one symbol.");
        }

        if (request.MaxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.MaxConcurrency), "Batch price history MaxConcurrency must be greater than zero.");
        }

        ValidatePriceHistoryRequest(new PriceHistoryRequest
        {
            Symbol = request.Symbols[0],
            Range = request.Range,
            Interval = request.Interval,
            Start = request.Start,
            End = request.End,
            IncludePrePost = request.IncludePrePost,
            AdjustmentMode = request.AdjustmentMode,
            TimestampMode = request.TimestampMode
        });
    }

    private static void ValidateBatchCompanyProfileRequest(BatchCompanyProfileRequest request)
    {
        if (request.Symbols is null || request.Symbols.Count == 0)
        {
            throw new ArgumentException("Batch company profiles requires at least one symbol.");
        }

        if (request.MaxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.MaxConcurrency), "Batch company profiles MaxConcurrency must be greater than zero.");
        }
    }

    private static void ValidateBatchTickerNewsRequest(BatchTickerNewsRequest request)
    {
        if (request.Symbols is null || request.Symbols.Count == 0)
        {
            throw new ArgumentException("Batch ticker news requires at least one symbol.");
        }

        if (request.MaxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.MaxConcurrency), "Batch ticker news MaxConcurrency must be greater than zero.");
        }

        ValidateTickerNewsRequest(request.Count);
    }

    private static void ConfigureDefaultHeaders(HttpClient httpClient, YahooFinanceClientOptions options)
    {
        if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        }

        if (!httpClient.DefaultRequestHeaders.AcceptLanguage.Any())
        {
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(options.AcceptLanguage);
        }
    }

    private sealed record BatchExecutionState(YahooFinanceClient Client);

    private sealed record BatchPriceHistoryExecutionState(YahooFinanceClient Client, BatchPriceHistoryRequest Request);

    private sealed record BatchTickerNewsExecutionState(YahooFinanceClient Client, BatchTickerNewsRequest Request);
}