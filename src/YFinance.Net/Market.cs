namespace YFinance.Net;

public sealed class Market : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    public Market(MarketRegion region, YahooFinanceClient? client = null)
    {
        Region = region;
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    public Market(string region, YahooFinanceClient? client = null)
        : this(ParseRegion(region), client)
    {
    }

    public MarketRegion Region { get; }

    public Task<MarketSummaryResult> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetMarketSummaryAsync(Region, cancellationToken);
    }

    public Task<MarketStatus?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetMarketStatusAsync(Region, cancellationToken);
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }

    private static MarketRegion ParseRegion(string region)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);

        if (Enum.TryParse<MarketRegion>(region, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentOutOfRangeException(nameof(region), region, $"Unknown market region '{region}'.");
    }
}