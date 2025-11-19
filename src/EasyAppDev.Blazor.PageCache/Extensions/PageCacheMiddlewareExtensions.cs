using Microsoft.AspNetCore.Builder;
using EasyAppDev.Blazor.PageCache.Middleware;

namespace EasyAppDev.Blazor.PageCache.Extensions;

/// <summary>
/// Extension methods for configuring page cache middleware.
/// </summary>
public static class PageCacheMiddlewareExtensions
{
    /// <summary>
    /// Adds page cache middleware to the application pipeline.
    /// This registers both the serve middleware (for cache hits) and capture middleware (for caching responses).
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    /// <example>
    /// <code>
    /// app.UsePageCache();
    /// </code>
    /// </example>
    public static IApplicationBuilder UsePageCache(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // First middleware: serves cached content if available
        app.UseMiddleware<PageCacheServeMiddleware>();

        // Second middleware: captures and caches rendered output
        app.UseMiddleware<PageCacheCaptureMiddleware>();

        return app;
    }
}
