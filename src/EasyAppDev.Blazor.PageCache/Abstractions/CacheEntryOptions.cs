namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Options for cache entry storage.
/// </summary>
public sealed class CacheEntryOptions
{
    /// <summary>
    /// Gets or sets the absolute expiration time relative to now.
    /// </summary>
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    /// <summary>
    /// Gets or sets the sliding expiration time.
    /// </summary>
    /// <remarks>
    /// The cache entry will be evicted if not accessed within this timespan.
    /// </remarks>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Gets or sets the size of the cache entry for size-based eviction.
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Gets or sets a callback to invoke when the entry is evicted.
    /// </summary>
    public Action<string, object?, EvictionReason>? PostEvictionCallback { get; set; }
}
