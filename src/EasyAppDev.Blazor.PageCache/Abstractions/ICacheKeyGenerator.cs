using Microsoft.AspNetCore.Http;
using EasyAppDev.Blazor.PageCache.Attributes;

namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Generates cache keys for HTTP requests.
/// </summary>
/// <remarks>
/// Implementing this interface allows customization of cache key generation logic,
/// enabling strategies such as hash-based keys, encrypted keys, or custom formatting.
/// </remarks>
public interface ICacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key for the given HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="attribute">Optional PageCache attribute with configuration.</param>
    /// <returns>A unique cache key string.</returns>
    string GenerateKey(HttpContext context, PageCacheAttribute? attribute = null);

    /// <summary>
    /// Determines if a request should be cached based on its characteristics.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns><c>true</c> if the request is cacheable; otherwise, <c>false</c>.</returns>
    bool IsCacheable(HttpContext context);
}
