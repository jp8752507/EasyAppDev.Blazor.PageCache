using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EasyAppDev.Blazor.PageCache.Configuration;
using EasyAppDev.Blazor.PageCache.Services;

namespace EasyAppDev.Blazor.PageCache.Tests.Integration;

/// <summary>
/// Integration tests for cache invalidation scenarios.
/// </summary>
public class InvalidationIntegrationTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IPageCacheService _cacheService;
    private readonly IPageCacheInvalidator _invalidator;
    private readonly PageCacheOptions _options;

    public InvalidationIntegrationTests()
    {
        var services = new ServiceCollection();

        _options = new PageCacheOptions
        {
            Enabled = true,
            CacheKeyPrefix = "page:",
            DefaultDurationSeconds = 300,
            EnableStatistics = true
        };

        services.AddMemoryCache();
        services.AddSingleton<AsyncKeyedLock>();
        services.AddSingleton<IPageCacheService, PageCacheService>();
        services.AddSingleton<IPageCacheInvalidator, PageCacheInvalidator>();
        services.AddSingleton(Options.Create(_options));
        services.AddSingleton<ILogger<PageCacheService>>(_ => NullLogger<PageCacheService>.Instance);
        services.AddSingleton<ILogger<PageCacheInvalidator>>(_ => NullLogger<PageCacheInvalidator>.Instance);

        _serviceProvider = services.BuildServiceProvider();
        _cacheService = _serviceProvider.GetRequiredService<IPageCacheService>();
        _invalidator = _serviceProvider.GetRequiredService<IPageCacheInvalidator>();
    }

    [Fact]
    public async Task InvalidateRoute_AfterCaching_RemovesEntry()
    {
        // Arrange
        const string route = "/products/123";
        const string cacheKey = "page:/products/123";
        const string html = "<html><body>Product 123</body></html>";

        await _cacheService.SetCachedHtmlAsync(cacheKey, html, 300);

        // Register with invalidator
        if (_invalidator is PageCacheInvalidator invalidatorImpl)
        {
            invalidatorImpl.RegisterCacheKey(route, cacheKey);
        }

        // Verify cached
        var cachedHtml = _cacheService.GetCachedHtml(cacheKey);
        cachedHtml.Should().Be(html);

        // Act
        var removed = _invalidator.InvalidateRoute(route);

        // Assert
        removed.Should().BeTrue();
        _cacheService.GetCachedHtml(cacheKey).Should().BeNull();
    }

    [Fact]
    public async Task InvalidatePattern_RemovesMatchingEntries()
    {
        // Arrange
        const string route1 = "/blog/post-1";
        const string route2 = "/blog/post-2";
        const string route3 = "/about";

        const string cacheKey1 = "page:/blog/post-1";
        const string cacheKey2 = "page:/blog/post-2";
        const string cacheKey3 = "page:/about";

        await _cacheService.SetCachedHtmlAsync(cacheKey1, "<html>Post 1</html>", 300);
        await _cacheService.SetCachedHtmlAsync(cacheKey2, "<html>Post 2</html>", 300);
        await _cacheService.SetCachedHtmlAsync(cacheKey3, "<html>About</html>", 300);

        if (_invalidator is PageCacheInvalidator invalidatorImpl)
        {
            invalidatorImpl.RegisterCacheKey(route1, cacheKey1);
            invalidatorImpl.RegisterCacheKey(route2, cacheKey2);
            invalidatorImpl.RegisterCacheKey(route3, cacheKey3);
        }

        // Act
        var removed = _invalidator.InvalidatePattern("/blog/*");

        // Assert
        removed.Should().Be(2);
        _cacheService.GetCachedHtml(cacheKey1).Should().BeNull();
        _cacheService.GetCachedHtml(cacheKey2).Should().BeNull();
        _cacheService.GetCachedHtml(cacheKey3).Should().NotBeNull();
    }

    [Fact]
    public async Task InvalidateByTag_RemovesOnlyTaggedEntries()
    {
        // Arrange
        const string route1 = "/products/1";
        const string route2 = "/products/2";
        const string route3 = "/about";

        const string cacheKey1 = "page:/products/1";
        const string cacheKey2 = "page:/products/2";
        const string cacheKey3 = "page:/about";

        await _cacheService.SetCachedHtmlAsync(cacheKey1, "<html>Product 1</html>", 300);
        await _cacheService.SetCachedHtmlAsync(cacheKey2, "<html>Product 2</html>", 300);
        await _cacheService.SetCachedHtmlAsync(cacheKey3, "<html>About</html>", 300);

        if (_invalidator is PageCacheInvalidator invalidatorImpl)
        {
            invalidatorImpl.RegisterCacheKey(route1, cacheKey1, new[] { "products" });
            invalidatorImpl.RegisterCacheKey(route2, cacheKey2, new[] { "products" });
            invalidatorImpl.RegisterCacheKey(route3, cacheKey3, null);
        }

        // Act
        var removed = _invalidator.InvalidateByTag("products");

        // Assert
        removed.Should().Be(2);
        _cacheService.GetCachedHtml(cacheKey1).Should().BeNull();
        _cacheService.GetCachedHtml(cacheKey2).Should().BeNull();
        _cacheService.GetCachedHtml(cacheKey3).Should().NotBeNull();
    }

    [Fact]
    public async Task ClearAll_RemovesAllCachedEntries()
    {
        // Arrange
        const string cacheKey1 = "page:/products/1";
        const string cacheKey2 = "page:/about";
        const string cacheKey3 = "page:/contact";

        await _cacheService.SetCachedHtmlAsync(cacheKey1, "<html>1</html>", 300);
        await _cacheService.SetCachedHtmlAsync(cacheKey2, "<html>2</html>", 300);
        await _cacheService.SetCachedHtmlAsync(cacheKey3, "<html>3</html>", 300);

        if (_invalidator is PageCacheInvalidator invalidatorImpl)
        {
            invalidatorImpl.RegisterCacheKey("/products/1", cacheKey1);
            invalidatorImpl.RegisterCacheKey("/about", cacheKey2);
            invalidatorImpl.RegisterCacheKey("/contact", cacheKey3);
        }

        // Act
        var removed = _invalidator.ClearAll();

        // Assert
        removed.Should().Be(3);
        _cacheService.GetCachedHtml(cacheKey1).Should().BeNull();
        _cacheService.GetCachedHtml(cacheKey2).Should().BeNull();
        _cacheService.GetCachedHtml(cacheKey3).Should().BeNull();
        _invalidator.GetCachedRoutes().Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleTagsOnSameEntry_InvalidatesCorrectly()
    {
        // Arrange
        const string route = "/products/1";
        const string cacheKey = "page:/products/1";
        const string html = "<html>Product 1</html>";

        await _cacheService.SetCachedHtmlAsync(cacheKey, html, 300);

        if (_invalidator is PageCacheInvalidator invalidatorImpl)
        {
            invalidatorImpl.RegisterCacheKey(route, cacheKey, new[] { "products", "catalog", "featured" });
        }

        // Act - invalidate by one tag
        var removed = _invalidator.InvalidateByTag("featured");

        // Assert
        removed.Should().Be(1);
        _cacheService.GetCachedHtml(cacheKey).Should().BeNull();
    }

    [Fact]
    public void GetCachedRoutes_ReturnsAllRegisteredRoutes()
    {
        // Arrange
        if (_invalidator is PageCacheInvalidator invalidatorImpl)
        {
            invalidatorImpl.RegisterCacheKey("/products/1", "page:/products/1");
            invalidatorImpl.RegisterCacheKey("/products/2", "page:/products/2");
            invalidatorImpl.RegisterCacheKey("/about", "page:/about");
        }

        // Act
        var routes = _invalidator.GetCachedRoutes();

        // Assert
        routes.Should().HaveCount(3);
        routes.Should().Contain("/products/1");
        routes.Should().Contain("/products/2");
        routes.Should().Contain("/about");
    }

    [Fact]
    public async Task Statistics_TrackInvalidations_Correctly()
    {
        // Arrange
        const string cacheKey = "page:/test";
        await _cacheService.SetCachedHtmlAsync(cacheKey, "<html>Test</html>", 300);

        // Get initial hit
        _cacheService.GetCachedHtml(cacheKey);

        // Get initial stats
        var stats1 = (_cacheService as PageCacheService)?.GetStatistics();
        stats1?.HitCount.Should().Be(1);
        stats1?.CachedEntries.Should().Be(1);

        // Act - invalidate
        if (_invalidator is PageCacheInvalidator invalidatorImpl)
        {
            invalidatorImpl.RegisterCacheKey("/test", cacheKey);
        }
        _invalidator.InvalidateRoute("/test");

        // Assert
        var stats2 = (_cacheService as PageCacheService)?.GetStatistics();
        stats2?.CachedEntries.Should().Be(0);
    }
}
