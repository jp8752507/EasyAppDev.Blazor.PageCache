namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Context information for cache set events.
/// </summary>
public sealed class CacheSetContext
{
    /// <summary>
    /// Gets or sets the cache key that was set.
    /// </summary>
    public required string CacheKey { get; init; }

    /// <summary>
    /// Gets or sets the size of the cached content in bytes.
    /// </summary>
    public long ContentSizeBytes { get; init; }

    /// <summary>
    /// Gets or sets the original (uncompressed) size in bytes.
    /// </summary>
    public long OriginalSizeBytes { get; init; }

    /// <summary>
    /// Gets or sets whether the content was compressed.
    /// </summary>
    public bool IsCompressed { get; init; }

    /// <summary>
    /// Gets or sets the cache duration in seconds.
    /// </summary>
    public int DurationSeconds { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the cache set operation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
