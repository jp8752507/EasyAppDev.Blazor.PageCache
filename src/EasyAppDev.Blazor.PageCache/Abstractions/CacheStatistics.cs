namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Represents global cache statistics used by eviction policies.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>
    /// Gets or sets the total number of cached entries.
    /// </summary>
    public int TotalEntries { get; init; }

    /// <summary>
    /// Gets or sets the total cache size in bytes.
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Gets or sets the cache memory limit in bytes (if configured).
    /// </summary>
    public long? MemoryLimitBytes { get; init; }

    /// <summary>
    /// Gets or sets the cache hit rate (0.0 to 1.0).
    /// </summary>
    public double HitRate { get; init; }

    /// <summary>
    /// Gets or sets the cache uptime.
    /// </summary>
    public TimeSpan Uptime { get; init; }
}
