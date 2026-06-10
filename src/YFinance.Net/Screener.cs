namespace YFinance.Net;

/// <summary>
/// Convenience facade for querying one of Yahoo Finance's predefined screeners.
/// </summary>
public sealed class Screener : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    /// <summary>
    /// Initializes a screener facade for a Yahoo screener identifier.
    /// </summary>
    /// <param name="screenId">Yahoo screener identifier such as <c>day_gainers</c>.</param>
    /// <param name="client">Optional shared client instance. When omitted, the screener owns a new <see cref="YahooFinanceClient"/>.</param>
    public Screener(string screenId, YahooFinanceClient? client = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(screenId);

        ScreenId = screenId;
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    /// <summary>
    /// Initializes a screener facade for a known predefined screener.
    /// </summary>
    /// <param name="screenId">Predefined screener identifier.</param>
    /// <param name="client">Optional shared client instance. When omitted, the screener owns a new <see cref="YahooFinanceClient"/>.</param>
    public Screener(PredefinedScreenerId screenId, YahooFinanceClient? client = null)
        : this(screenId.ToWireValue(), client)
    {
    }

    /// <summary>
    /// Gets the Yahoo screener identifier represented by this facade.
    /// </summary>
    public string ScreenId { get; }

    /// <summary>
    /// Gets the screener result using Yahoo's default settings.
    /// </summary>
    public Task<ScreenerResult> GetAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetPredefinedScreenerAsync(ScreenId, cancellationToken);
    }

    /// <summary>
    /// Gets the screener result with explicit paging parameters.
    /// </summary>
    /// <param name="count">Maximum number of rows to request.</param>
    /// <param name="offset">Result offset for paging.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    public Task<ScreenerResult> GetAsync(int count, int offset = 0, CancellationToken cancellationToken = default)
    {
        return _client.GetPredefinedScreenerAsync(ScreenId, new PredefinedScreenerOptions
        {
            Count = count,
            Offset = offset
        }, cancellationToken);
    }

    /// <summary>
    /// Gets the screener result using explicit Yahoo screener options.
    /// </summary>
    /// <param name="options">Optional screener options such as count, offset, language, and region.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    public Task<ScreenerResult> GetAsync(PredefinedScreenerOptions? options, CancellationToken cancellationToken = default)
    {
        return _client.GetPredefinedScreenerAsync(ScreenId, options, cancellationToken);
    }

    /// <summary>
    /// Disposes the owned client when this screener created it.
    /// </summary>
    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }
}