namespace YFinance.Net;

/// <summary>
/// Convenience facade for querying Yahoo Finance market-level summary and status data.
/// </summary>
public sealed class Market : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    /// <summary>
    /// Initializes a market facade for the specified region.
    /// </summary>
    /// <param name="region">Market region to query.</param>
    /// <param name="client">Optional shared client instance. When omitted, the market facade owns a new <see cref="YahooFinanceClient"/>.</param>
    public Market(MarketRegion region, YahooFinanceClient? client = null)
    {
        Region = region;
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    /// <summary>
    /// Initializes a market facade for the specified region name.
    /// </summary>
    /// <param name="region">Region name matching a <see cref="MarketRegion"/> value.</param>
    /// <param name="client">Optional shared client instance. When omitted, the market facade owns a new <see cref="YahooFinanceClient"/>.</param>
    public Market(string region, YahooFinanceClient? client = null)
        : this(ParseRegion(region), client)
    {
    }

    /// <summary>
    /// Gets the market region represented by this facade.
    /// </summary>
    public MarketRegion Region { get; }

    /// <summary>
    /// Gets market summary data for <see cref="Region"/>.
    /// </summary>
    public Task<MarketSummaryResult> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetMarketSummaryAsync(Region, cancellationToken);
    }

    /// <summary>
    /// Gets current market status data for <see cref="Region"/>.
    /// </summary>
    public Task<MarketStatus?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetMarketStatusAsync(Region, cancellationToken);
    }

    /// <summary>
    /// Disposes the owned client when this market facade created it.
    /// </summary>
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