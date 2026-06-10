namespace YFinance.Net;

/// <summary>
/// Convenience facade for querying Yahoo Finance industry details.
/// </summary>
public sealed class Industry : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    /// <summary>
    /// Initializes an industry facade for a key and region.
    /// </summary>
    public Industry(string key, string region = "US", YahooFinanceClient? client = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Key = key;
        Region = Sector.NormalizeRegion(region);
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    /// <summary>
    /// Gets the industry key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the normalized region code.
    /// </summary>
    public string Region { get; }

    /// <summary>
    /// Gets industry details using the default cache behavior.
    /// </summary>
    public Task<IndustryDetails> GetAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetIndustryAsync(Key, Region, cancellationToken);
    }

    /// <summary>
    /// Gets industry details using an explicit cache mode.
    /// </summary>
    public Task<IndustryDetails> GetAsync(YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        return _client.GetIndustryAsync(Key, Region, cacheMode, cancellationToken);
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
}