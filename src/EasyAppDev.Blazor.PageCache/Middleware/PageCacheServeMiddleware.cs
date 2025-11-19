using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Services;

namespace EasyAppDev.Blazor.PageCache.Middleware;

/// <summary>
/// Middleware that serves pre-rendered HTML from cache when available.
/// </summary>
public class PageCacheServeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IPageCacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<PageCacheServeMiddleware> _logger;

    public PageCacheServeMiddleware(
        RequestDelegate next,
        IPageCacheService cacheService,
        ICacheKeyGenerator keyGenerator,
        ILogger<PageCacheServeMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process GET requests
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!_keyGenerator.IsCacheable(context))
        {
            await _next(context);
            return;
        }

        // Generate cache key (without vary parameters for now - they'll be added by the filter)
        var cacheKey = _keyGenerator.GenerateKey(context);

        var cachedHtml = _cacheService.GetCachedHtml(cacheKey);

        if (cachedHtml != null)
        {
            // Cache hit - serve cached content
            _logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers["X-Page-Cache"] = "HIT";

            await context.Response.WriteAsync(cachedHtml);
            return; // Short-circuit the pipeline
        }

        // Cache miss - continue to next middleware
        _logger.LogTrace("Cache MISS for key: {CacheKey}", cacheKey);
        await _next(context);
    }
}
