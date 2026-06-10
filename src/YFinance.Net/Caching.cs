using System.Collections.Concurrent;

namespace YFinance.Net;

/// <summary>
/// Controls how cache-aware APIs use cached Yahoo Finance data.
/// </summary>
public enum YFinanceCacheMode
{
    /// <summary>
    /// Uses the default cache behavior configured on the client.
    /// </summary>
    Default,
    /// <summary>
    /// Reads from cache when possible.
    /// </summary>
    UseCache,
    /// <summary>
    /// Forces a refresh from Yahoo Finance and updates cache entries.
    /// </summary>
    Refresh,
    /// <summary>
    /// Bypasses cache reads and writes.
    /// </summary>
    BypassCache
}

/// <summary>
/// Abstraction for storing cached values used by the client.
/// </summary>
public interface IYFinanceCacheStore
{
    /// <summary>
    /// Attempts to retrieve a cached value for the specified key.
    /// </summary>
    /// <typeparam name="T">Expected cached value type.</typeparam>
    /// <param name="key">Cache key to retrieve.</param>
    /// <param name="value">Receives the cached value when present and of the expected type.</param>
    /// <returns><see langword="true"/> when a matching cached value is found; otherwise <see langword="false"/>.</returns>
    bool TryGetValue<T>(string key, out T? value);

    /// <summary>
    /// Stores a value in the cache.
    /// </summary>
    /// <typeparam name="T">Value type being cached.</typeparam>
    /// <param name="key">Cache key to store.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="ttl">Optional time to live for the cached entry.</param>
    void Set<T>(string key, T value, TimeSpan? ttl = null);

    /// <summary>
    /// Removes a cached value.
    /// </summary>
    /// <param name="key">Cache key to remove.</param>
    void Remove(string key);
}

/// <summary>
/// Configures client-side caching for cache-aware Yahoo Finance operations.
/// </summary>
public sealed record YahooFinanceCacheOptions
{
    /// <summary>
    /// Gets the cache store implementation used by the client.
    /// </summary>
    public IYFinanceCacheStore? Store { get; init; } = new MemoryYFinanceCacheStore();

    /// <summary>
    /// Gets the default cache behavior applied by APIs that accept <see cref="YFinanceCacheMode.Default"/>.
    /// </summary>
    public YFinanceCacheMode DefaultMode { get; init; } = YFinanceCacheMode.UseCache;

    /// <summary>
    /// Gets whether option expiration date responses should be cached.
    /// </summary>
    public bool EnableOptionExpirationCache { get; init; } = true;

    /// <summary>
    /// Gets the time to live for cached option expiration date responses.
    /// </summary>
    public TimeSpan OptionExpirationCacheTtl { get; init; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Gets whether sector and industry responses should be cached.
    /// </summary>
    public bool EnableDomainCache { get; init; } = true;

    /// <summary>
    /// Gets the time to live for cached sector and industry responses.
    /// </summary>
    public TimeSpan DomainCacheTtl { get; init; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// In-memory cache store implementation backed by a concurrent dictionary.
/// </summary>
public sealed class MemoryYFinanceCacheStore : IYFinanceCacheStore
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Set<T>(string key, T value, TimeSpan? ttl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        DateTimeOffset? expiresAtUtc = ttl is { } ttlValue
            ? (DateTimeOffset?)DateTimeOffset.UtcNow.Add(ttlValue)
            : null;

        _entries[key] = new CacheEntry(value, expiresAtUtc);
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _entries.TryRemove(key, out _);
    }

    private readonly record struct CacheEntry(object? Value, DateTimeOffset? ExpiresAtUtc);
}