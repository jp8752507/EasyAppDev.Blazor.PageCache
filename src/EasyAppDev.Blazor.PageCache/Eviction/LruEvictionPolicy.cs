using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Eviction;

/// <summary>
/// Least Recently Used (LRU) eviction policy.
/// </summary>
public sealed class LruEvictionPolicy : IEvictionPolicy
{
    private readonly TimeSpan? _maxAge;

    public LruEvictionPolicy(TimeSpan? maxAge = null)
    {
        _maxAge = maxAge;
    }

    /// <inheritdoc />
    public bool ShouldEvict(CacheEntry entry, CacheStatistics stats)
    {
        if (_maxAge.HasValue)
        {
            var age = DateTimeOffset.UtcNow - entry.LastAccessedAt;
            return age > _maxAge.Value;
        }

        if (stats.MemoryLimitBytes.HasValue &&
            stats.TotalSizeBytes > stats.MemoryLimitBytes.Value)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public int GetPriority(CacheEntry entry)
    {
        var timeSinceAccess = DateTimeOffset.UtcNow - entry.LastAccessedAt;
        return (int)timeSinceAccess.TotalSeconds;
    }

    /// <inheritdoc />
    public void OnAccess(CacheEntry entry)
    {
        entry.LastAccessedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public void OnAdd(CacheEntry entry)
    {
    }

    /// <inheritdoc />
    public void OnRemove(CacheEntry entry)
    {
    }
}
