namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Defines lifecycle event hooks for page cache operations.
/// </summary>
public interface IPageCacheEvents
{
    /// <summary>
    /// Called when a cache hit occurs.
    /// </summary>
    /// <param name="context">The cache hit context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnCacheHitAsync(CacheHitContext context);

    /// <summary>
    /// Called when a cache miss occurs.
    /// </summary>
    /// <param name="context">The cache miss context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnCacheMissAsync(CacheMissContext context);

    /// <summary>
    /// Called when a new entry is set in the cache.
    /// </summary>
    /// <param name="context">The cache set context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnCacheSetAsync(CacheSetContext context);

    /// <summary>
    /// Called when cache entries are invalidated.
    /// </summary>
    /// <param name="context">The invalidation context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task OnCacheInvalidatedAsync(InvalidationContext context);
}
