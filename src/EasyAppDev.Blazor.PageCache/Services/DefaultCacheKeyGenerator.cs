using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Attributes;
using EasyAppDev.Blazor.PageCache.Configuration;

namespace EasyAppDev.Blazor.PageCache.Services;

/// <summary>
/// Default implementation of <see cref="ICacheKeyGenerator"/> that generates cache keys based on request characteristics.
/// </summary>
public sealed class DefaultCacheKeyGenerator : ICacheKeyGenerator
{
    private readonly PageCacheOptions _options;
    private readonly ILogger<DefaultCacheKeyGenerator> _logger;

    public DefaultCacheKeyGenerator(
        IOptions<PageCacheOptions> options,
        ILogger<DefaultCacheKeyGenerator> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string GenerateKey(HttpContext context, PageCacheAttribute? attribute = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var keyBuilder = new StringBuilder(_options.CacheKeyPrefix);

        var path = NormalizePath(context.Request.Path);
        keyBuilder.Append(path);

        AppendRouteValues(context, keyBuilder);

        if (attribute?.VaryByQueryKeys?.Length > 0)
        {
            AppendQueryStringVariations(context, keyBuilder, attribute.VaryByQueryKeys);
        }

        if (!string.IsNullOrWhiteSpace(attribute?.VaryByHeader))
        {
            AppendHeaderVariation(context, keyBuilder, attribute.VaryByHeader);
        }

        if (_options.VaryByCulture)
        {
            AppendCultureVariation(keyBuilder);
        }

        var cacheKey = keyBuilder.ToString();

        _logger.LogDebug("Generated cache key: {CacheKey} for path: {Path}", cacheKey, path);

        return cacheKey;
    }

    /// <inheritdoc />
    public bool IsCacheable(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.Request;

        // Only cache GET requests
        if (!HttpMethods.IsGet(request.Method))
        {
            _logger.LogDebug("Request not cacheable: Method is {Method}", request.Method);
            return false;
        }

        // Check if user is authenticated
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Check if endpoint has PageCacheAttribute with CacheForAuthenticatedUsers = true
            var endpoint = context.GetEndpoint();
            var pageCacheAttr = endpoint?.Metadata.GetMetadata<PageCacheAttribute>();

            if (pageCacheAttr?.CacheForAuthenticatedUsers != true)
            {
                _logger.LogDebug("Request not cacheable: User is authenticated and CacheForAuthenticatedUsers is not enabled");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Normalizes a path by converting to lowercase and ensuring consistent format.
    /// </summary>
    private static string NormalizePath(PathString path)
    {
        var normalized = path.Value?.ToLowerInvariant() ?? "/";

        // Remove trailing slash except for root
        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }

    /// <summary>
    /// Appends route values to the cache key for dynamic routes.
    /// </summary>
    private static void AppendRouteValues(HttpContext context, StringBuilder keyBuilder)
    {
        var routeValues = context.GetRouteData()?.Values;
        if (routeValues == null || routeValues.Count == 0)
        {
            return;
        }

        // Sort route values for consistent key generation
        var sortedRouteValues = routeValues
            .Where(kvp => kvp.Key != "page" && kvp.Value != null) // Exclude 'page' (Blazor internal)
            .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sortedRouteValues.Count == 0)
        {
            return;
        }

        keyBuilder.Append("?rv=");
        foreach (var kvp in sortedRouteValues)
        {
            keyBuilder.Append(kvp.Key.ToLowerInvariant());
            keyBuilder.Append('=');
            keyBuilder.Append(kvp.Value?.ToString()?.ToLowerInvariant() ?? "");
            keyBuilder.Append('&');
        }

        // Remove trailing '&' (defensive check)
        if (keyBuilder.Length > 0 && keyBuilder[keyBuilder.Length - 1] == '&')
        {
            keyBuilder.Length--;
        }
    }

    /// <summary>
    /// Appends specified query string parameters to the cache key.
    /// </summary>
    private void AppendQueryStringVariations(
        HttpContext context,
        StringBuilder keyBuilder,
        string[] varyByQueryKeys)
    {
        var query = context.Request.Query;
        if (query.Count == 0)
        {
            return;
        }

        var relevantParams = new List<(string Key, string Value)>();

        foreach (var queryKey in varyByQueryKeys)
        {
            if (query.TryGetValue(queryKey, out var values))
            {
                var value = values.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    relevantParams.Add((queryKey.ToLowerInvariant(), value.ToLowerInvariant()));
                }
            }
        }

        // Filter out ignored parameters
        relevantParams = relevantParams
            .Where(p => !_options.IgnoredQueryParameters.Contains(p.Key))
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (relevantParams.Count == 0)
        {
            return;
        }

        keyBuilder.Append("?qs=");
        foreach (var (key, value) in relevantParams)
        {
            keyBuilder.Append(key);
            keyBuilder.Append('=');
            keyBuilder.Append(value);
            keyBuilder.Append('&');
        }

        // Remove trailing '&' (defensive check)
        if (keyBuilder.Length > 0 && keyBuilder[keyBuilder.Length - 1] == '&')
        {
            keyBuilder.Length--;
        }
    }

    /// <summary>
    /// Appends header value to the cache key.
    /// </summary>
    private static void AppendHeaderVariation(
        HttpContext context,
        StringBuilder keyBuilder,
        string headerName)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var headerValue))
        {
            var value = headerValue.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                keyBuilder.Append("?h=");
                keyBuilder.Append(headerName.ToLowerInvariant());
                keyBuilder.Append('=');
                keyBuilder.Append(value.ToLowerInvariant());
            }
        }
    }

    /// <summary>
    /// Appends current culture to the cache key.
    /// </summary>
    private static void AppendCultureVariation(StringBuilder keyBuilder)
    {
        var culture = CultureInfo.CurrentCulture.Name;
        keyBuilder.Append("?c=");
        keyBuilder.Append(culture.ToLowerInvariant());
    }
}
