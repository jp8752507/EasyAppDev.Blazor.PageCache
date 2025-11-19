using Xunit;
using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Eviction;

namespace EasyAppDev.Blazor.PageCache.Tests.Eviction;

[Trait("Category", TestCategories.Unit)]
public sealed class LfuEvictionPolicyTests
{
    [Fact]
    public void OnAccess_IncrementsAccessCount()
    {
        // Arrange
        var policy = new LfuEvictionPolicy();
        var entry = new CacheEntry
        {
            Key = "test",
            AccessCount = 5
        };

        // Act
        policy.OnAccess(entry);

        // Assert
        Assert.Equal(6, entry.AccessCount);
    }

    [Fact]
    public void OnAdd_InitializesAccessCount()
    {
        // Arrange
        var policy = new LfuEvictionPolicy();
        var entry = new CacheEntry
        {
            Key = "test",
            AccessCount = 0
        };

        // Act
        policy.OnAdd(entry);

        // Assert
        Assert.Equal(1, entry.AccessCount);
    }

    [Fact]
    public void GetPriority_LessFrequentlyUsed_HigherPriority()
    {
        // Arrange
        var policy = new LfuEvictionPolicy();
        var frequentEntry = new CacheEntry
        {
            Key = "frequent",
            AccessCount = 100
        };
        var infrequentEntry = new CacheEntry
        {
            Key = "infrequent",
            AccessCount = 5
        };

        // Act
        var frequentPriority = policy.GetPriority(frequentEntry);
        var infrequentPriority = policy.GetPriority(infrequentEntry);

        // Assert
        Assert.True(infrequentPriority > frequentPriority,
            "Less frequently accessed entries should have higher priority for eviction");
    }

    [Fact]
    public void ShouldEvict_WithThreshold_BelowThreshold_ReturnsTrue()
    {
        // Arrange
        var threshold = 10;
        var policy = new LfuEvictionPolicy(threshold);
        var entry = new CacheEntry
        {
            Key = "test",
            AccessCount = 5
        };
        var stats = new CacheStatistics();

        // Act
        var result = policy.ShouldEvict(entry, stats);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldEvict_WithThreshold_AboveThreshold_ReturnsFalse()
    {
        // Arrange
        var threshold = 10;
        var policy = new LfuEvictionPolicy(threshold);
        var entry = new CacheEntry
        {
            Key = "test",
            AccessCount = 15
        };
        var stats = new CacheStatistics();

        // Act
        var result = policy.ShouldEvict(entry, stats);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldEvict_OverMemoryLimit_ReturnsTrue()
    {
        // Arrange
        var policy = new LfuEvictionPolicy();
        var entry = new CacheEntry
        {
            Key = "test",
            AccessCount = 100
        };
        var stats = new CacheStatistics
        {
            TotalSizeBytes = 1024 * 1024 * 150, // 150 MB
            MemoryLimitBytes = 1024 * 1024 * 100 // 100 MB limit
        };

        // Act
        var result = policy.ShouldEvict(entry, stats);

        // Assert
        Assert.True(result);
    }
}
