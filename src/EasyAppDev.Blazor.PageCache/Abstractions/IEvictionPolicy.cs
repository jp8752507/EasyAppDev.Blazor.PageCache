namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Defines a contract for cache eviction policies.
/// </summary>
/// <remarks>
/// Eviction policies determine which cache entries should be removed when
/// the cache reaches capacity or needs to free up resources.
/// </remarks>
public interface IEvictionPolicy
{
    /// <summary>
    /// Determines whether a cache entry should be evicted.
    /// </summary>
    /// <param name="entry">The cache entry to evaluate.</param>
    /// <param name="stats">Current cache statistics.</param>
    /// <returns><c>true</c> if the entry should be evicted; otherwise, <c>false</c>.</returns>
    bool ShouldEvict(CacheEntry entry, CacheStatistics stats);

    /// <summary>
    /// Gets the eviction priority for a cache entry.
    /// </summary>
    /// <param name="entry">The cache entry to evaluate.</param>
    /// <returns>
    /// A priority score where higher values indicate higher priority for eviction.
    /// </returns>
    /// <remarks>
    /// This method is used to rank entries when multiple entries need to be compared.
    /// Entries with higher scores will be evicted first.
    /// </remarks>
    int GetPriority(CacheEntry entry);

    /// <summary>
    /// Called when a cache entry is accessed (cache hit).
    /// </summary>
    /// <param name="entry">The cache entry that was accessed.</param>
    /// <remarks>
    /// This allows eviction policies to update their internal state based on access patterns.
    /// For example, LRU and LFU policies use this to track access information.
    /// </remarks>
    void OnAccess(CacheEntry entry);

    /// <summary>
    /// Called when a new cache entry is added.
    /// </summary>
    /// <param name="entry">The cache entry that was added.</param>
    void OnAdd(CacheEntry entry);

    /// <summary>
    /// Called when a cache entry is removed.
    /// </summary>
    /// <param name="entry">The cache entry that was removed.</param>
    void OnRemove(CacheEntry entry);
}
