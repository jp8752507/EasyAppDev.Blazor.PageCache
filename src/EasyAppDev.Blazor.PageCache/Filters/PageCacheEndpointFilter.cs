using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Attributes;
using EasyAppDev.Blazor.PageCache.Configuration;
using EasyAppDev.Blazor.PageCache.Services;

namespace EasyAppDev.Blazor.PageCache.Filters;

/// <summary>
/// Endpoint filter that intercepts requests and serves cached HTML when available.
/// </summary>
public sealed partial class PageCacheEndpointFilter : IEndpointFilter
{
    private readonly IPageCacheService _cacheService;
    private readonly IPageCacheInvalidator _invalidator;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly PageCacheOptions _options;
    private readonly ILogger<PageCacheEndpointFilter> _logger;

    public PageCacheEndpointFilter(
        IPageCacheService cacheService,
        IPageCacheInvalidator invalidator,
        ICacheKeyGenerator keyGenerator,
        IOptions<PageCacheOptions> options,
        ILogger<PageCacheEndpointFilter> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _invalidator = invalidator ?? throw new ArgumentNullException(nameof(invalidator));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!_options.Enabled)
        {
            return await next(context);
        }

        // Get PageCache attribute from endpoint metadata
        var cacheAttribute = httpContext.GetEndpoint()?.Metadata
            .GetMetadata<PageCacheAttribute>();

        if (cacheAttribute == null)
        {
            // No [PageCache] attribute - skip caching
            return await next(context);
        }

        if (!_keyGenerator.IsCacheable(httpContext))
        {
            return await next(context);
        }

        // Generate cache key
        var cacheKey = _keyGenerator.GenerateKey(httpContext, cacheAttribute);

        var cachedHtml = _cacheService.GetCachedHtml(cacheKey);
        if (cachedHtml != null)
        {
            // Cache hit - serve cached response
            await ServeCachedResponseAsync(httpContext, cachedHtml);
            return null; // Indicates response has been written
        }

        // Cache miss - acquire lock to prevent stampede
        using (await _cacheService.AcquireLockAsync(cacheKey, httpContext.RequestAborted))
        {
            // Double-check cache (another request might have populated it)
            cachedHtml = _cacheService.GetCachedHtml(cacheKey);
            if (cachedHtml != null)
            {
                await ServeCachedResponseAsync(httpContext, cachedHtml);
                return null;
            }

            // Render and capture response
            var html = await CaptureResponseAsync(httpContext, context, next);

            // Cache successful responses only
            if (ShouldCacheResponse(httpContext))
            {
                var duration = cacheAttribute.Duration > 0
                    ? cacheAttribute.Duration
                    : _options.DefaultDurationSeconds;

                await _cacheService.SetCachedHtmlAsync(cacheKey, html, duration);

                var route = httpContext.Request.Path.Value ?? "/";
                if (_invalidator is PageCacheInvalidator invalidatorImpl)
                {
                    invalidatorImpl.RegisterCacheKey(route, cacheKey, cacheAttribute.Tags);
                }
            }

            return null; // Response already written
        }
    }

    /// <summary>
    /// Serves cached HTML to the response.
    /// </summary>
    private async Task ServeCachedResponseAsync(HttpContext context, string html)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
        context.Response.Headers["X-Page-Cache"] = "HIT";

        await context.Response.WriteAsync(html, Encoding.UTF8, context.RequestAborted);

        LogServedFromCache(context.Request.Path);
    }

    /// <summary>
    /// Captures the rendered response HTML.
    /// Pre-sizes the MemoryStream for better performance.
    /// </summary>
    private async Task<string> CaptureResponseAsync(
        HttpContext httpContext,
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var originalBody = httpContext.Response.Body;

        try
        {
            // Pre-size MemoryStream to avoid multiple allocations (typical HTML size: 64KB)
            using var memoryStream = new MemoryStream(capacity: 64 * 1024);
            httpContext.Response.Body = memoryStream;

            try
            {
                // Let Blazor render to the memory stream
                await next(context);
            }
            catch
            {
                // Restore original body before re-throwing
                httpContext.Response.Body = originalBody;
                throw;
            }

            // Read captured HTML
            memoryStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            var html = await reader.ReadToEndAsync();

            // Write to original response
            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBody, httpContext.RequestAborted);

            httpContext.Response.Headers["X-Page-Cache"] = "MISS";

            LogRenderedAndCached(httpContext.Request.Path, html.Length);

            return html;
        }
        finally
        {
            httpContext.Response.Body = originalBody;
        }
    }

    /// <summary>
    /// Determines if the response should be cached.
    /// </summary>
    private bool ShouldCacheResponse(HttpContext context)
    {
        if (_options.CacheOnlySuccessfulResponses)
        {
            return context.Response.StatusCode == 200;
        }

        return _options.CacheableStatusCodes.Contains(context.Response.StatusCode);
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Debug,
        Message = "Served from cache: {Path}")]
    private partial void LogServedFromCache(string path);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Debug,
        Message = "Rendered and cached: {Path}, Size: {SizeBytes} bytes")]
    private partial void LogRenderedAndCached(string path, int sizeBytes);
}
