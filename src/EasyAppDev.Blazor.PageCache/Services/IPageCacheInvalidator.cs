namespace EasyAppDev.Blazor.PageCache.Services;

/// <summary>
/// Service for invalidating cached pages.
/// </summary>
/// <example>
/// <code>
/// public class BlogService
/// {
///     private readonly IPageCacheInvalidator _invalidator;
///
///     public async Task UpdatePost(string slug)
///     {
///         await _db.SaveChangesAsync();
///
///         // Invalidate specific blog post
///         _invalidator.InvalidateRoute($"/blog/{slug}");
///
///         // Invalidate all blog pages
///         _invalidator.InvalidatePattern("/blog/*");
///     }
/// }
/// </code>
/// </example>
public interface IPageCacheInvalidator
{
    /// <summary>
    /// Invalidates the cache for a specific route.
    /// </summary>
    /// <param name="route">The route to invalidate (e.g., "/blog/my-post").</param>
    /// <returns><c>true</c> if the cache entry was found and removed; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// _invalidator.InvalidateRoute("/about");
    /// _invalidator.InvalidateRoute("/products/123");
    /// </code>
    /// </example>
    bool InvalidateRoute(string route);

    /// <summary>
    /// Invalidates all cache entries matching the specified pattern.
    /// </summary>
    /// <param name="pattern">
    /// The pattern to match. Supports wildcard (*) matching.
    /// Examples: "/blog/*", "/products/*", "*/admin"
    /// </param>
    /// <returns>The number of cache entries invalidated.</returns>
    /// <example>
    /// <code>
    /// // Invalidate all blog pages
    /// var count = _invalidator.InvalidatePattern("/blog/*");
    ///
    /// // Invalidate all pages
    /// _invalidator.InvalidatePattern("*");
    /// </code>
    /// </example>
    int InvalidatePattern(string pattern);

    /// <summary>
    /// Invalidates all cache entries with the specified tag.
    /// </summary>
    /// <param name="tag">The tag to invalidate.</param>
    /// <returns>The number of cache entries invalidated.</returns>
    /// <remarks>
    /// Tags must be specified when caching via <see cref="Attributes.PageCacheAttribute.Tags"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Invalidate all pages tagged with "products"
    /// _invalidator.InvalidateByTag("products");
    /// </code>
    /// </example>
    int InvalidateByTag(string tag);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    /// <returns>The number of cache entries cleared.</returns>
    /// <example>
    /// <code>
    /// var count = _invalidator.ClearAll();
    /// _logger.LogInformation("Cleared {Count} cache entries", count);
    /// </code>
    /// </example>
    int ClearAll();

    /// <summary>
    /// Gets a list of all currently cached routes.
    /// </summary>
    /// <returns>A collection of cached route paths.</returns>
    /// <example>
    /// <code>
    /// var cachedRoutes = _invalidator.GetCachedRoutes();
    /// foreach (var route in cachedRoutes)
    /// {
    ///     Console.WriteLine(route);
    /// }
    /// </code>
    /// </example>
    IReadOnlyCollection<string> GetCachedRoutes();
}
