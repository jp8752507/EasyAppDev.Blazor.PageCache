namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Context information for cache miss events.
/// </summary>
public sealed class CacheMissContext
{
    /// <summary>
    /// Gets or sets the cache key that was missed.
    /// </summary>
    public required string CacheKey { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the cache miss.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
