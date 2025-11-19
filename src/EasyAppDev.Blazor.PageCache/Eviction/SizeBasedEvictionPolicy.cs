using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Eviction;

/// <summary>
/// Size-based eviction policy that prioritizes evicting larger entries.
/// </summary>
public sealed class SizeBasedEvictionPolicy : IEvictionPolicy
{
    private readonly long? _maxEntrySizeBytes;
    private readonly EvictionStrategy _strategy;

    /// <summary>
    /// Defines the strategy for size-based eviction.
    /// </summary>
    public enum EvictionStrategy
    {
        LargestFirst,
        SmallestFirst
    }

    public SizeBasedEvictionPolicy(
        long? maxEntrySizeBytes = null,
        EvictionStrategy strategy = EvictionStrategy.LargestFirst)
    {
        _maxEntrySizeBytes = maxEntrySizeBytes;
        _strategy = strategy;
    }

    /// <inheritdoc />
    public bool ShouldEvict(CacheEntry entry, CacheStatistics stats)
    {
        // Always evict entries that exceed the max size threshold
        if (_maxEntrySizeBytes.HasValue && entry.SizeBytes > _maxEntrySizeBytes.Value)
        {
            return true;
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
        return _strategy switch
        {
            EvictionStrategy.LargestFirst =>
                // Larger entries get higher priority (evicted first)
                (int)Math.Min(entry.SizeBytes, int.MaxValue),

            EvictionStrategy.SmallestFirst =>
                // Smaller entries get higher priority (evicted first)
                (int)Math.Min(long.MaxValue - entry.SizeBytes, int.MaxValue),

            _ => 0
        };
    }

    /// <inheritdoc />
    public void OnAccess(CacheEntry entry)
    {
        // Update last accessed time for potential hybrid strategies
        entry.LastAccessedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public void OnAdd(CacheEntry entry)
    {
        // Nothing specific to do for size-based policy on add
    }

    /// <inheritdoc />
    public void OnRemove(CacheEntry entry)
    {
        // Nothing specific to do for size-based policy on remove
    }
}
