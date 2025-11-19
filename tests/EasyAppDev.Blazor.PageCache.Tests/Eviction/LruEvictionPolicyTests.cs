using Xunit;
using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Eviction;

namespace EasyAppDev.Blazor.PageCache.Tests.Eviction;

[Trait("Category", TestCategories.Unit)]
public sealed class LruEvictionPolicyTests
{
    [Fact]
    public void OnAccess_UpdatesLastAccessedTime()
    {
        // Arrange
        var policy = new LruEvictionPolicy();
        var entry = new CacheEntry
        {
            Key = "test",
            LastAccessedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        };
        var originalTime = entry.LastAccessedAt;

        // Act
        Thread.Sleep(10); // Small delay to ensure time difference
        policy.OnAccess(entry);

        // Assert
        Assert.True(entry.LastAccessedAt > originalTime);
    }

    [Fact]
    public void GetPriority_OlderEntries_HigherPriority()
    {
        // Arrange
        var policy = new LruEvictionPolicy();
        var oldEntry = new CacheEntry
        {
            Key = "old",
            LastAccessedAt = DateTimeOffset.UtcNow.AddHours(-2)
        };
        var newEntry = new CacheEntry
        {
            Key = "new",
            LastAccessedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        // Act
        var oldPriority = policy.GetPriority(oldEntry);
        var newPriority = policy.GetPriority(newEntry);

        // Assert
        Assert.True(oldPriority > newPriority, "Older entries should have higher priority for eviction");
    }

    [Fact]
    public void ShouldEvict_WithMaxAge_ExpiredEntry_ReturnsTrue()
    {
        // Arrange
        var maxAge = TimeSpan.FromMinutes(30);
        var policy = new LruEvictionPolicy(maxAge);
        var entry = new CacheEntry
        {
            Key = "test",
            LastAccessedAt = DateTimeOffset.UtcNow.AddHours(-1) // Older than max age
        };
        var stats = new CacheStatistics();

        // Act
        var result = policy.ShouldEvict(entry, stats);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldEvict_WithMaxAge_RecentEntry_ReturnsFalse()
    {
        // Arrange
        var maxAge = TimeSpan.FromMinutes(30);
        var policy = new LruEvictionPolicy(maxAge);
        var entry = new CacheEntry
        {
            Key = "test",
            LastAccessedAt = DateTimeOffset.UtcNow.AddMinutes(-5) // Within max age
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
        var policy = new LruEvictionPolicy();
        var entry = new CacheEntry
        {
            Key = "test",
            LastAccessedAt = DateTimeOffset.UtcNow
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
