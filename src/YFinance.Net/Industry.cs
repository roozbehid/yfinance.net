namespace YFinance.Net;

public sealed class Industry : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    public Industry(string key, string region = "US", YahooFinanceClient? client = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Key = key;
        Region = Sector.NormalizeRegion(region);
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    public string Key { get; }

    public string Region { get; }

    public Task<IndustryDetails> GetAsync(CancellationToken cancellationToken = default)
    {
        return _client.GetIndustryAsync(Key, Region, cancellationToken);
    }

    public Task<IndustryDetails> GetAsync(YFinanceCacheMode cacheMode, CancellationToken cancellationToken = default)
    {
        return _client.GetIndustryAsync(Key, Region, cacheMode, cancellationToken);
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }
}