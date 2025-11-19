namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Represents a cache entry with metadata for eviction policy decisions.
/// </summary>
public sealed class CacheEntry
{
    /// <summary>
    /// Gets or sets the cache key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets or sets the size of the cached value in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the entry was last accessed.
    /// </summary>
    public DateTimeOffset LastAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of times this entry has been accessed.
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// Gets or sets the absolute expiration time.
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; init; }

    /// <summary>
    /// Gets or sets custom metadata for this entry.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
