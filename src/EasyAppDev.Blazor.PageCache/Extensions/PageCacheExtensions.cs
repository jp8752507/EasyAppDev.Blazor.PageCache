using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using EasyAppDev.Blazor.PageCache.Services;
using EasyAppDev.Blazor.PageCache.Diagnostics;

namespace EasyAppDev.Blazor.PageCache.Extensions;

/// <summary>
/// Extension methods for page caching.
/// </summary>
public static class PageCacheExtensions
{
    /// <summary>
    /// Checks if the current request was served from cache.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns><c>true</c> if served from cache; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (HttpContext.IsFromCache())
    /// {
    ///     _logger.LogInformation("Page served from cache");
    /// }
    /// </code>
    /// </example>
    public static bool IsFromCache(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Response.Headers.TryGetValue("X-Page-Cache", out var value) &&
               value == "HIT";
    }

    /// <summary>
    /// Invalidates the cache for the current request route.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="invalidator">The cache invalidator.</param>
    /// <returns><c>true</c> if invalidated; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// var invalidator = HttpContext.RequestServices.GetRequiredService&lt;IPageCacheInvalidator&gt;();
    /// if (HttpContext.InvalidateCurrentRoute(invalidator))
    /// {
    ///     _logger.LogInformation("Cache invalidated for current route");
    /// }
    /// </code>
    /// </example>
    public static bool InvalidateCurrentRoute(
        this HttpContext context,
        IPageCacheInvalidator invalidator)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(invalidator);

        var route = context.Request.Path.Value ?? "/";
        return invalidator.InvalidateRoute(route);
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <returns>Current cache statistics.</returns>
    /// <example>
    /// <code>
    /// var stats = serviceProvider.GetCacheStats();
    /// Console.WriteLine(stats.GetDetailedReport());
    /// </code>
    /// </example>
    public static PageCacheStats GetCacheStats(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var cacheService = services.GetService<IPageCacheService>() as PageCacheService;
        return cacheService?.GetStatistics() ?? new PageCacheStats();
    }
}
