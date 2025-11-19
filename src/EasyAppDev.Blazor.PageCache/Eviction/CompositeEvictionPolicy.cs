using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Eviction;

/// <summary>
/// Composite eviction policy that combines multiple policies.
/// </summary>
public sealed class CompositeEvictionPolicy : IEvictionPolicy
{
    private readonly IEvictionPolicy[] _policies;
    private readonly CompositionMode _mode;

    /// <summary>
    /// Defines how multiple policies are combined.
    /// </summary>
    public enum CompositionMode
    {
        Any,
        All,
        WeightedPriority
    }

    public CompositeEvictionPolicy(IEnumerable<IEvictionPolicy> policies, CompositionMode mode = CompositionMode.WeightedPriority)
    {
        _policies = policies?.ToArray() ?? throw new ArgumentNullException(nameof(policies));

        if (_policies.Length == 0)
        {
            throw new ArgumentException("At least one policy is required", nameof(policies));
        }

        _mode = mode;
    }

    public CompositeEvictionPolicy(
        CompositionMode mode,
        params IEvictionPolicy[] policies)
        : this(policies, mode)
    {
    }

    /// <inheritdoc />
    public bool ShouldEvict(CacheEntry entry, CacheStatistics stats)
    {
        return _mode switch
        {
            CompositionMode.Any =>
                _policies.Any(p => p.ShouldEvict(entry, stats)),

            CompositionMode.All =>
                _policies.All(p => p.ShouldEvict(entry, stats)),

            CompositionMode.WeightedPriority =>
                // In weighted mode, check if any policy thinks it should evict
                _policies.Any(p => p.ShouldEvict(entry, stats)),

            _ => false
        };
    }

    /// <inheritdoc />
    public int GetPriority(CacheEntry entry)
    {
        return _mode switch
        {
            CompositionMode.Any =>
                // Return the highest priority from all policies
                _policies.Max(p => p.GetPriority(entry)),

            CompositionMode.All =>
                // Return the average priority
                (int)_policies.Average(p => p.GetPriority(entry)),

            CompositionMode.WeightedPriority =>
                // Sum all priorities (weighted equally)
                _policies.Sum(p => p.GetPriority(entry)) / _policies.Length,

            _ => 0
        };
    }

    /// <inheritdoc />
    public void OnAccess(CacheEntry entry)
    {
        foreach (var policy in _policies)
        {
            policy.OnAccess(entry);
        }
    }

    /// <inheritdoc />
    public void OnAdd(CacheEntry entry)
    {
        foreach (var policy in _policies)
        {
            policy.OnAdd(entry);
        }
    }

    /// <inheritdoc />
    public void OnRemove(CacheEntry entry)
    {
        foreach (var policy in _policies)
        {
            policy.OnRemove(entry);
        }
    }
}
