namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Abstraction for cache storage backends.
/// </summary>
/// <remarks>
/// This interface enables pluggable cache storage implementations
/// such as memory cache, Redis, SQL Server, or hybrid caching strategies.
/// </remarks>
public interface ICacheStorage
{
    /// <summary>
    /// Gets a cached value from storage.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached value if found; otherwise, <c>null</c>.</returns>
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a value in the cache with the specified options.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="options">Cache entry options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific cache entry.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The pattern to match (supports * wildcard).</param>
    /// <param name="maxCount">Maximum number of entries to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of entries removed.</returns>
    ValueTask<int> RemoveByPatternAsync(string pattern, int maxCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
