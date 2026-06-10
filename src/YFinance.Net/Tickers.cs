namespace YFinance.Net;

public sealed class Tickers : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;
    private readonly IReadOnlyDictionary<string, Ticker> _items;

    public Tickers(IEnumerable<string> symbols, YahooFinanceClient? client = null)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;

        var normalizedSymbols = symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedSymbols.Length == 0)
        {
            throw new ArgumentException("Tickers requires at least one symbol.", nameof(symbols));
        }

        Symbols = normalizedSymbols;
        _items = normalizedSymbols.ToDictionary(symbol => symbol, symbol => new Ticker(symbol, _client), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> Symbols { get; }

    public IReadOnlyDictionary<string, Ticker> Items => _items;

    public Ticker this[string symbol] => _items[symbol];

    public Task<BatchPriceHistoryResult> GetHistoriesAsync(
        string range = "1mo",
        string interval = "1d",
        bool includePrePost = false,
        PriceAdjustmentMode adjustmentMode = PriceAdjustmentMode.None,
        PriceTimestampMode timestampMode = PriceTimestampMode.Utc,
        int maxConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        return _client.GetPriceHistoriesAsync(new BatchPriceHistoryRequest
        {
            Symbols = Symbols,
            Range = range,
            Interval = interval,
            IncludePrePost = includePrePost,
            AdjustmentMode = adjustmentMode,
            TimestampMode = timestampMode,
            MaxConcurrency = maxConcurrency
        }, cancellationToken);
    }

    public Task<BatchCompanyProfileResult> GetCompanyProfilesAsync(
        int maxConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        return _client.GetCompanyProfilesAsync(new BatchCompanyProfileRequest(Symbols, maxConcurrency), cancellationToken);
    }

    public Task<BatchTickerNewsResult> GetNewsAsync(
        int count = 10,
        TickerNewsTab tab = TickerNewsTab.News,
        int maxConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        return _client.GetNewsAsync(new BatchTickerNewsRequest(Symbols, count, tab, maxConcurrency), cancellationToken);
    }

    public Task<YahooLiveStream> OpenLiveStreamAsync(
        YahooLiveStreamOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return OpenLiveStreamAsync(new YahooLiveStream(options), cancellationToken);
    }

    internal async Task<YahooLiveStream> OpenLiveStreamAsync(
        YahooLiveStream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            await stream.SubscribeAsync(Symbols, cancellationToken).ConfigureAwait(false);
            return stream;
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public void Dispose()
    {
        foreach (var ticker in _items.Values)
        {
            ticker.Dispose();
        }

        if (_ownsClient)
        {
            _client.Dispose();
        }
    }
}