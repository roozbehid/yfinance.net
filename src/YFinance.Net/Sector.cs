namespace YFinance.Net;

public sealed class Sector : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    public Sector(string key, string region = "US", YahooFinanceClient? client = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Key = key;
        Region = NormalizeRegion(region);
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    public string Key { get; }

    public string Region { get; }

    public Task<SectorDetails> GetAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetSectorAsync(Key, Region, cancellationToken);
    }

    public Task<SectorDetails> GetAsync(YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        return _client.GetSectorAsync(Key, Region, cacheMode, cancellationToken);
    }

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