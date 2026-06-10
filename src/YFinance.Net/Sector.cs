namespace YFinance.Net;

/// <summary>
/// Convenience facade for querying Yahoo Finance sector details.
/// </summary>
public sealed class Sector : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    /// <summary>
    /// Initializes a sector facade for a key and region.
    /// </summary>
    public Sector(string key, string region = "US", YahooFinanceClient? client = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Key = key;
        Region = NormalizeRegion(region);
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    /// <summary>
    /// Gets the sector key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the normalized region code.
    /// </summary>
    public string Region { get; }

    /// <summary>
    /// Gets sector details using the default cache behavior.
    /// </summary>
    public Task<SectorDetails> GetAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetSectorAsync(Key, Region, cancellationToken);
    }

    /// <summary>
    /// Gets sector details using an explicit cache mode.
    /// </summary>
    public Task<SectorDetails> GetAsync(YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        return _client.GetSectorAsync(Key, Region, cacheMode, cancellationToken);
    }

    /// <summary>
    /// Disposes the owned client when this facade created it.
    /// </summary>
    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }

    internal static string NormalizeRegion(string region)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        return region.Trim().ToUpperInvariant();
    }
}