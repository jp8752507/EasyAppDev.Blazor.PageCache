namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Context information for cache invalidation events.
/// </summary>
public sealed class InvalidationContext
{
    /// <summary>
    /// Gets or sets the cache key or pattern that was invalidated.
    /// </summary>
    public required string KeyOrPattern { get; init; }

    /// <summary>
    /// Gets or sets whether this was a pattern-based invalidation.
    /// </summary>
    public bool IsPattern { get; init; }

    /// <summary>
    /// Gets or sets the number of entries removed.
    /// </summary>
    public int EntriesRemoved { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the invalidation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
