using FluentAssertions;
using EasyAppDev.Blazor.PageCache.Diagnostics;

namespace EasyAppDev.Blazor.PageCache.Tests.Diagnostics;

[Trait("Category", TestCategories.Unit)]
public class PageCacheStatsTests
{
    [Fact]
    public void HitRate_WithZeroRequests_ReturnsZero()
    {
        // Arrange
        var stats = new PageCacheStats
        {
            HitCount = 0,
            MissCount = 0,
            TotalRequests = 0,
            HitRate = 0
        };

        // Assert
        stats.HitRate.Should().Be(0);
    }

    [Fact]
    public void HitRate_WithAllHits_ReturnsOne()
    {
        // Arrange
        var stats = new PageCacheStats
        {
            HitCount = 100,
            MissCount = 0,
            TotalRequests = 100,
            HitRate = 1.0
        };

        // Assert
        stats.HitRate.Should().Be(1.0);
    }

    [Fact]
    public void HitRate_With50PercentHits_ReturnsHalf()
    {
        // Arrange
        var stats = new PageCacheStats
        {
            HitCount = 50,
            MissCount = 50,
            TotalRequests = 100,
            HitRate = 0.5
        };

        // Assert
        stats.HitRate.Should().Be(0.5);
    }

    [Fact]
    public void CacheSizeMB_CalculatesCorrectly()
    {
        // Arrange
        var stats = new PageCacheStats
        {
            CacheSizeBytes = 1024 * 1024 * 10 // 10 MB
        };

        // Assert
        stats.CacheSizeMB.Should().BeApproximately(10.0, 0.01);
    }

    [Fact]
    public void AveragePageSizeBytes_WithMultipleEntries_CalculatesCorrectly()
    {
        // Arrange
        var stats = new PageCacheStats
        {
            CacheSizeBytes = 10000,
            CachedEntries = 10
        };

        // Assert
        stats.AveragePageSizeBytes.Should().Be(1000.0);
    }

    [Fact]
    public void AveragePageSizeBytes_WithZeroEntries_ReturnsZero()
    {
        // Arrange
        var stats = new PageCacheStats
        {
            CacheSizeBytes = 10000,
            CachedEntries = 0
        };

        // Assert
        stats.AveragePageSizeBytes.Should().Be(0);
    }

    [Fact]
    public void Duration_CalculatesTimeSinceStart()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var stats = new PageCacheStats
        {
            StartTime = startTime
        };

        // Assert
        stats.Duration.TotalMinutes.Should().BeGreaterThanOrEqualTo(4.9);
        stats.Duration.TotalMinutes.Should().BeLessThanOrEqualTo(5.1);
    }

    [Fact]
    public void RequestsPerSecond_WithTraffic_CalculatesCorrectly()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-10);
        var stats = new PageCacheStats
        {
            StartTime = startTime,
            TotalRequests = 100
        };

        // Assert - Should be approximately 10 requests per second
        stats.RequestsPerSecond.Should().BeGreaterThanOrEqualTo(9.0);
        stats.RequestsPerSecond.Should().BeLessThanOrEqualTo(11.0);
    }

    [Fact]
    public void RequestsPerSecond_WithZeroDuration_ReturnsZero()
    {
        // Arrange
        var stats = new PageCacheStats
        {
            StartTime = DateTimeOffset.UtcNow,
            TotalRequests = 100
        };

        // Wait a tiny bit to ensure duration > 0
        Thread.Sleep(10);

        // Assert
        stats.RequestsPerSecond.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ToString_ReturnsFormattedSummary()
    {
        // Arrange
        var stats = new PageCacheStats
        {
            HitCount = 80,
            MissCount = 20,
            HitRate = 0.8,
            CachedEntries = 50,
            CacheSizeBytes = 1024 * 1024 * 5 // 5 MB
        };

        // Act
        var result = stats.ToString();

        // Assert
        result.Should().Contain("80 hits");
        result.Should().Contain("20 misses");
        result.Should().Contain("80.00%");
        result.Should().Contain("50 entries");
        result.Should().Contain("5.00 MB");
    }

    [Fact]
    public void GetDetailedReport_ReturnsComprehensiveReport()
    {
        // Arrange
        var stats = new PageCacheStats
        {
            HitCount = 900,
            MissCount = 100,
            TotalRequests = 1000,
            HitRate = 0.9,
            CachedEntries = 25,
            CacheSizeBytes = 1024 * 1024 * 2, // 2 MB
            EvictionCount = 5,
            StartTime = DateTimeOffset.UtcNow.AddHours(-1)
        };

        // Act
        var report = stats.GetDetailedReport();

        // Assert
        report.Should().Contain("Page Cache Statistics");
        report.Should().Contain("Hit Rate:");
        report.Should().Contain("90.00%");
        report.Should().Contain("Total Requests:");
        report.Should().Contain("1,000");
        report.Should().Contain("Cache Hits:");
        report.Should().Contain("900");
        report.Should().Contain("Cache Misses:");
        report.Should().Contain("100");
        report.Should().Contain("Cached Entries:");
        report.Should().Contain("25");
        report.Should().Contain("Cache Size:");
        report.Should().Contain("2.00 MB");
        report.Should().Contain("Evictions:");
        report.Should().Contain("5");
        report.Should().Contain("Uptime:");
        report.Should().Contain("Requests/Second:");
    }

    [Fact]
    public void GetDetailedReport_WithZeroStats_HandlesGracefully()
    {
        // Arrange
        var stats = new PageCacheStats
        {
            StartTime = DateTimeOffset.UtcNow
        };

        // Act
        var report = stats.GetDetailedReport();

        // Assert
        report.Should().NotBeNullOrEmpty();
        report.Should().Contain("0.00%"); // Hit rate
        report.Should().Contain("0 bytes"); // Avg page size
    }
}
