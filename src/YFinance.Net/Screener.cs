namespace YFinance.Net;

public sealed class Screener : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    public Screener(string screenId, YahooFinanceClient? client = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(screenId);

        ScreenId = screenId;
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    public Screener(PredefinedScreenerId screenId, YahooFinanceClient? client = null)
        : this(screenId.ToWireValue(), client)
    {
    }

    public string ScreenId { get; }

    public Task<ScreenerResult> GetAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetPredefinedScreenerAsync(ScreenId, cancellationToken);
    }

    public Task<ScreenerResult> GetAsync(int count, int offset = 0, CancellationToken cancellationToken = default)
    {
        return _client.GetPredefinedScreenerAsync(ScreenId, new PredefinedScreenerOptions
        {
            Count = count,
            Offset = offset
        }, cancellationToken);
    }

    public Task<ScreenerResult> GetAsync(PredefinedScreenerOptions? options, CancellationToken cancellationToken = default)
    {
        return _client.GetPredefinedScreenerAsync(ScreenId, options, cancellationToken);
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }
}