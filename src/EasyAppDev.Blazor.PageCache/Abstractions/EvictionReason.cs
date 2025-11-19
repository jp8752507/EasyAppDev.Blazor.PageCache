namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Reason why a cache entry was evicted.
/// </summary>
public enum EvictionReason
{
    /// <summary>
    /// The cache entry was explicitly removed.
    /// </summary>
    Removed,

    /// <summary>
    /// The cache entry was replaced with a new value.
    /// </summary>
    Replaced,

    /// <summary>
    /// The cache entry expired due to absolute or sliding expiration.
    /// </summary>
    Expired,

    /// <summary>
    /// The cache entry was evicted due to capacity constraints.
    /// </summary>
    Capacity,

    /// <summary>
    /// A token associated with the cache entry triggered eviction.
    /// </summary>
    TokenExpired,

    /// <summary>
    /// The cache entry was evicted for another reason.
    /// </summary>
    None
}
