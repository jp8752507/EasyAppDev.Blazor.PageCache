using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Eviction;

/// <summary>
/// Least Frequently Used (LFU) eviction policy.
/// </summary>
public sealed class LfuEvictionPolicy : IEvictionPolicy
{
    private readonly int? _minAccessThreshold;

    public LfuEvictionPolicy(int? minAccessThreshold = null)
    {
        _minAccessThreshold = minAccessThreshold;
    }

    /// <inheritdoc />
    public bool ShouldEvict(CacheEntry entry, CacheStatistics stats)
    {
        if (_minAccessThreshold.HasValue)
        {
            return entry.AccessCount < _minAccessThreshold.Value;
        }

        // If we're over memory limit, evict based on priority
        if (stats.MemoryLimitBytes.HasValue &&
            stats.TotalSizeBytes > stats.MemoryLimitBytes.Value)
        {
            return true; // Will be decided by priority
        }

        return false;
    }

    /// <inheritdoc />
    public int GetPriority(CacheEntry entry)
    {
        // Higher priority = evict first
        // Lower access counts get higher priority for eviction
        // Use max value minus access count so less-accessed items have higher priority
        return int.MaxValue - entry.AccessCount;
    }

    /// <inheritdoc />
    public void OnAccess(CacheEntry entry)
    {
        // Increment access count
        entry.AccessCount++;

        // Also update last accessed time for hybrid strategies
        entry.LastAccessedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public void OnAdd(CacheEntry entry)
    {
        // Initialize access count to 1 (the current access)
        entry.AccessCount = 1;
    }

    /// <inheritdoc />
    public void OnRemove(CacheEntry entry)
    {
        // Nothing specific to do for LFU on remove
    }
}
