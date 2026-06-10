namespace YFinance.Net;

/// <summary>
/// Convenience facade for performing batch operations across a set of ticker symbols.
/// </summary>
public sealed class Tickers : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;
    private readonly IReadOnlyDictionary<string, Ticker> _items;

    /// <summary>
    /// Initializes a ticker collection facade for the specified symbols.
    /// </summary>
    /// <param name="symbols">Symbols to include. Empty and duplicate values are removed.</param>
    /// <param name="client">Optional shared client instance. When omitted, the collection owns a new <see cref="YahooFinanceClient"/>.</param>
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

    /// <summary>
    /// Gets the normalized symbol list represented by this collection.
    /// </summary>
    public IReadOnlyList<string> Symbols { get; }

    /// <summary>
    /// Gets ticker facades indexed by symbol.
    /// </summary>
    public IReadOnlyDictionary<string, Ticker> Items => _items;

    /// <summary>
    /// Gets the ticker facade for a symbol in this collection.
    /// </summary>
    public Ticker this[string symbol] => _items[symbol];

    /// <summary>
    /// Gets price history for all symbols in the collection.
    /// </summary>
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

    /// <summary>
    /// Gets company profiles for all symbols in the collection.
    /// </summary>
    public Task<BatchCompanyProfileResult> GetCompanyProfilesAsync(
        int maxConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        return _client.GetCompanyProfilesAsync(new BatchCompanyProfileRequest(Symbols, maxConcurrency), cancellationToken);
    }

    /// <summary>
    /// Gets ticker news for all symbols in the collection.
    /// </summary>
    public Task<BatchTickerNewsResult> GetNewsAsync(
        int count = 10,
        TickerNewsTab tab = TickerNewsTab.News,
        int maxConcurrency = 4,
        CancellationToken cancellationToken = default)
    {
        return _client.GetNewsAsync(new BatchTickerNewsRequest(Symbols, count, tab, maxConcurrency), cancellationToken);
    }

    /// <summary>
    /// Opens a live Yahoo Finance stream and subscribes all symbols in the collection.
    /// </summary>
    /// <param name="options">Optional live stream configuration.</param>
    /// <param name="cancellationToken">Token used to cancel connection and subscription.</param>
    /// <returns>An active <see cref="YahooLiveStream"/> already subscribed to all symbols.</returns>
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

    /// <summary>
    /// Disposes the ticker facades and owned client when applicable.
    /// </summary>
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