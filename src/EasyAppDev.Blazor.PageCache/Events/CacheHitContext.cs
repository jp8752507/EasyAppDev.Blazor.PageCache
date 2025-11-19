namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Context information for cache hit events.
/// </summary>
public sealed class CacheHitContext
{
    /// <summary>
    /// Gets or sets the cache key that was hit.
    /// </summary>
    public required string CacheKey { get; init; }

    /// <summary>
    /// Gets or sets the size of the cached content in bytes.
    /// </summary>
    public long ContentSizeBytes { get; init; }

    /// <summary>
    /// Gets or sets whether the content was compressed.
    /// </summary>
    public bool IsCompressed { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the cache hit.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
