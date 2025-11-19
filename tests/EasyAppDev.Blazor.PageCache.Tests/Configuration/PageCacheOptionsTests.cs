using FluentAssertions;
using EasyAppDev.Blazor.PageCache.Configuration;

namespace EasyAppDev.Blazor.PageCache.Tests.Configuration;

public class PageCacheOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var options = new PageCacheOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.DefaultDurationSeconds.Should().Be(300);
        options.MaxCacheSizeMB.Should().Be(100);
        options.SlidingExpirationSeconds.Should().BeNull();
        options.CompressCachedContent.Should().BeFalse();
        options.EnableStatistics.Should().BeTrue();
        options.CacheKeyPrefix.Should().Be("PageCache:");
        options.VaryByCulture.Should().BeTrue();
        options.MaxConcurrentCacheGenerations.Should().Be(1);
        options.CacheGenerationTimeoutSeconds.Should().Be(30);
        options.CacheOnlySuccessfulResponses.Should().BeTrue();
        options.IgnoredQueryParameters.Should().Contain("utm_source");
        options.IgnoredQueryParameters.Should().Contain("utm_medium");
        options.IgnoredQueryParameters.Should().Contain("utm_campaign");
        options.IgnoredQueryParameters.Should().Contain("utm_term");
        options.IgnoredQueryParameters.Should().Contain("utm_content");
        options.IgnoredQueryParameters.Should().Contain("fbclid");
        options.IgnoredQueryParameters.Should().Contain("gclid");
        options.CacheableStatusCodes.Should().Contain(200);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        // Arrange
        var options = new PageCacheOptions();

        // Act
        options.Enabled = false;
        options.DefaultDurationSeconds = 600;
        options.CacheKeyPrefix = "Custom:";
        options.MaxCacheSizeMB = 200;
        options.SlidingExpirationSeconds = 120;
        options.CompressCachedContent = true;
        options.EnableStatistics = false;
        options.VaryByCulture = false;
        options.MaxConcurrentCacheGenerations = 5;
        options.CacheGenerationTimeoutSeconds = 60;
        options.CacheOnlySuccessfulResponses = false;

        // Assert
        options.Enabled.Should().BeFalse();
        options.DefaultDurationSeconds.Should().Be(600);
        options.CacheKeyPrefix.Should().Be("Custom:");
        options.MaxCacheSizeMB.Should().Be(200);
        options.SlidingExpirationSeconds.Should().Be(120);
        options.CompressCachedContent.Should().BeTrue();
        options.EnableStatistics.Should().BeFalse();
        options.VaryByCulture.Should().BeFalse();
        options.MaxConcurrentCacheGenerations.Should().Be(5);
        options.CacheGenerationTimeoutSeconds.Should().Be(60);
        options.CacheOnlySuccessfulResponses.Should().BeFalse();
    }

    [Fact]
    public void IgnoredQueryParameters_CanBeModified()
    {
        // Arrange
        var options = new PageCacheOptions();

        // Act
        options.IgnoredQueryParameters.Add("custom_param");
        options.IgnoredQueryParameters.Remove("utm_source");

        // Assert
        options.IgnoredQueryParameters.Should().Contain("custom_param");
        options.IgnoredQueryParameters.Should().NotContain("utm_source");
    }

    [Fact]
    public void IgnoredQueryParameters_IsCaseInsensitive()
    {
        // Arrange
        var options = new PageCacheOptions();

        // Act & Assert
        options.IgnoredQueryParameters.Contains("UTM_SOURCE").Should().BeTrue();
        options.IgnoredQueryParameters.Contains("Utm_Source").Should().BeTrue();
    }

    [Fact]
    public void CacheableStatusCodes_CanBeModified()
    {
        // Arrange
        var options = new PageCacheOptions();

        // Act
        options.CacheableStatusCodes.Add(404);
        options.CacheableStatusCodes.Add(301);

        // Assert
        options.CacheableStatusCodes.Should().Contain(new[] { 200, 404, 301 });
    }

    [Fact]
    public void MaxCacheSizeMB_CanBeSetToNull()
    {
        // Arrange
        var options = new PageCacheOptions();

        // Act
        options.MaxCacheSizeMB = null;

        // Assert
        options.MaxCacheSizeMB.Should().BeNull();
    }

    [Fact]
    public void SlidingExpirationSeconds_CanBeSetToValue()
    {
        // Arrange
        var options = new PageCacheOptions();

        // Act
        options.SlidingExpirationSeconds = 180;

        // Assert
        options.SlidingExpirationSeconds.Should().Be(180);
    }
}
