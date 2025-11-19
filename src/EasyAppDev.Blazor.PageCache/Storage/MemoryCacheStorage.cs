using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using EasyAppDev.Blazor.PageCache.Abstractions;
using MemoryCacheEntryOptions = Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions;
using MsEvictionReason = Microsoft.Extensions.Caching.Memory.EvictionReason;

namespace EasyAppDev.Blazor.PageCache.Storage;

/// <summary>
/// In-memory cache storage implementation using <see cref="IMemoryCache"/>.
/// </summary>
public sealed class MemoryCacheStorage : ICacheStorage
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _cacheKeys = new();

    public MemoryCacheStorage(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        var result = _cache.TryGetValue<T>(key, out var value) && value != null
            ? value
            : default;

        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public ValueTask SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        var entryOptions = new MemoryCacheEntryOptions();

        if (options.AbsoluteExpirationRelativeToNow.HasValue)
        {
            entryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow.Value;
        }

        if (options.SlidingExpiration.HasValue)
        {
            entryOptions.SlidingExpiration = options.SlidingExpiration.Value;
        }

        if (options.Size.HasValue)
        {
            entryOptions.Size = options.Size.Value;
        }

        if (options.PostEvictionCallback != null)
        {
            var callback = options.PostEvictionCallback;
            entryOptions.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                if (key is string cacheKey)
                {
                    _cacheKeys.TryRemove(cacheKey, out _);
                    callback(cacheKey, value, MapEvictionReason(reason));
                }
            });
        }

        _cache.Set(key, value, entryOptions);
        _cacheKeys.TryAdd(key, 0);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        _cache.Remove(key);
        _cacheKeys.TryRemove(key, out _);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<int> RemoveByPatternAsync(string pattern, int maxCount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        cancellationToken.ThrowIfCancellationRequested();

        var removed = 0;
        var isWildcard = pattern.Contains('*');

        if (!isWildcard)
        {
            // Exact match
            if (_cacheKeys.ContainsKey(pattern))
            {
                _cache.Remove(pattern);
                _cacheKeys.TryRemove(pattern, out _);
                removed = 1;
            }
        }
        else
        {
            // Optimize common pattern: prefix matching (e.g., "page:*")
            if (pattern.EndsWith("*") && !pattern[..^1].Contains('*'))
            {
                // Simple prefix match - much faster than regex
                var prefix = pattern[..^1];
                var keysToRemove = _cacheKeys.Keys
                    .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Take(maxCount)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                    _cacheKeys.TryRemove(key, out _);
                    removed++;
                }
            }
            else if (pattern.StartsWith("*") && !pattern[1..].Contains('*'))
            {
                // Simple suffix match
                var suffix = pattern[1..];
                var keysToRemove = _cacheKeys.Keys
                    .Where(key => key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    .Take(maxCount)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                    _cacheKeys.TryRemove(key, out _);
                    removed++;
                }
            }
            else
            {
                // Complex wildcard pattern - use regex with timeout protection
                var regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*") + "$";

                var keysToRemove = _cacheKeys.Keys
                    .Where(key =>
                    {
                        try
                        {
                            return Regex.IsMatch(key, regexPattern,
                                RegexOptions.IgnoreCase | RegexOptions.Compiled,
                                TimeSpan.FromSeconds(1));
                        }
                        catch (RegexMatchTimeoutException)
                        {
                            return false;
                        }
                    })
                    .Take(maxCount)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                    _cacheKeys.TryRemove(key, out _);
                    removed++;
                }
            }
        }

        return ValueTask.FromResult(removed);
    }

    /// <inheritdoc />
    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var key in _cacheKeys.Keys.ToList())
        {
            _cache.Remove(key);
        }

        _cacheKeys.Clear();

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Gets all currently cached keys.
    /// </summary>
    /// <returns>A collection of cache keys.</returns>
    public IReadOnlyCollection<string> GetAllKeys()
    {
        return _cacheKeys.Keys.ToList();
    }

    /// <summary>
    /// Gets the number of cached entries.
    /// </summary>
    public int Count => _cacheKeys.Count;

    /// <summary>
    /// Maps Microsoft.Extensions.Caching.Memory.EvictionReason to our abstraction.
    /// </summary>
    private static Abstractions.EvictionReason MapEvictionReason(MsEvictionReason reason)
    {
        return reason switch
        {
            MsEvictionReason.Removed => Abstractions.EvictionReason.Removed,
            MsEvictionReason.Replaced => Abstractions.EvictionReason.Replaced,
            MsEvictionReason.Expired => Abstractions.EvictionReason.Expired,
            MsEvictionReason.Capacity => Abstractions.EvictionReason.Capacity,
            MsEvictionReason.TokenExpired => Abstractions.EvictionReason.TokenExpired,
            _ => Abstractions.EvictionReason.None
        };
    }
}
