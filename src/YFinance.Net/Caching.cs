using System.Collections.Concurrent;

namespace YFinance.Net;

public enum YFinanceCacheMode
{
    Default,
    UseCache,
    Refresh,
    BypassCache
}

public interface IYFinanceCacheStore
{
    bool TryGetValue<T>(string key, out T? value);

    void Set<T>(string key, T value, TimeSpan? ttl = null);

    void Remove(string key);
}

public sealed record YahooFinanceCacheOptions
{
    public IYFinanceCacheStore? Store { get; init; } = new MemoryYFinanceCacheStore();

    public YFinanceCacheMode DefaultMode { get; init; } = YFinanceCacheMode.UseCache;

    public bool EnableOptionExpirationCache { get; init; } = true;

    public TimeSpan OptionExpirationCacheTtl { get; init; } = TimeSpan.FromHours(6);

    public bool EnableDomainCache { get; init; } = true;

    public TimeSpan DomainCacheTtl { get; init; } = TimeSpan.FromMinutes(15);
}

public sealed class MemoryYFinanceCacheStore : IYFinanceCacheStore
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    public bool TryGetValue<T>(string key, out T? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_entries.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAtUtc is not null && entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                _entries.TryRemove(key, out _);
            }
            else if (entry.Value is T typed)
            {
                value = typed;
                return true;
            }
        }

        value = default;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan? ttl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        DateTimeOffset? expiresAtUtc = ttl is { } ttlValue
            ? (DateTimeOffset?)DateTimeOffset.UtcNow.Add(ttlValue)
            : null;

        _entries[key] = new CacheEntry(value, expiresAtUtc);
    }

    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _entries.TryRemove(key, out _);
    }

    private readonly record struct CacheEntry(object? Value, DateTimeOffset? ExpiresAtUtc);
}