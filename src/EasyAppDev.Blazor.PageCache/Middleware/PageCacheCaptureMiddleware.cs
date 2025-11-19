using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Configuration;
using EasyAppDev.Blazor.PageCache.Services;
using EasyAppDev.Blazor.PageCache.Attributes;

namespace EasyAppDev.Blazor.PageCache.Middleware;

/// <summary>
/// Middleware that captures rendered HTML output and stores it in the cache.
/// </summary>
public class PageCacheCaptureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IPageCacheService _cacheService;
    private readonly IPageCacheInvalidator _invalidator;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ILogger<PageCacheCaptureMiddleware> _logger;
    private readonly PageCacheOptions _options;

    public PageCacheCaptureMiddleware(
        RequestDelegate next,
        IPageCacheService cacheService,
        IPageCacheInvalidator invalidator,
        ICacheKeyGenerator keyGenerator,
        IOptions<PageCacheOptions> options,
        ILogger<PageCacheCaptureMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _invalidator = invalidator ?? throw new ArgumentNullException(nameof(invalidator));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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

        // Save the original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Create a new memory stream to capture the response
            using var captureStream = new MemoryStream();

            // Replace the response body with our capture stream
            context.Response.Body = captureStream;

            // Call the next middleware in the pipeline (rendering happens here)
            await _next(context);

            // After rendering, check if we should cache the response
            // Only cache successful responses (200 OK)
            if (context.Response.StatusCode == StatusCodes.Status200OK)
            {
                // Check if Content-Type is text/html
                var contentType = context.Response.ContentType ?? string.Empty;
                if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                {
                    // Read the captured HTML from the memory stream
                    captureStream.Position = 0;
                    string capturedHtml;

                    using (var reader = new StreamReader(captureStream, Encoding.UTF8, leaveOpen: true))
                    {
                        capturedHtml = await reader.ReadToEndAsync();
                    }

                    // Only cache if we captured some content
                    if (!string.IsNullOrWhiteSpace(capturedHtml))
                    {
                        try
                        {
                            // Generate cache key
                            var cacheKey = _keyGenerator.GenerateKey(context);
                            var route = context.Request.Path.Value ?? "/";

                            // Get endpoint metadata to check for PageCache attribute
                            var endpoint = context.GetEndpoint();
                            var pageCacheAttr = endpoint?.Metadata.GetMetadata<PageCacheAttribute>();

                            int duration = _options.DefaultDurationSeconds;
                            string[]? tags = null;

                            if (pageCacheAttr != null)
                            {
                                // Use duration from attribute if specified
                                if (pageCacheAttr.Duration > 0)
                                {
                                    duration = pageCacheAttr.Duration;
                                }

                                tags = pageCacheAttr.Tags;
                            }

                            await _cacheService.SetCachedHtmlAsync(cacheKey, capturedHtml, duration);

                            if (_invalidator is PageCacheInvalidator invalidatorImpl)
                            {
                                invalidatorImpl.RegisterCacheKey(route, cacheKey, tags);
                            }

                            context.Response.Headers["X-Page-Cache"] = "MISS";

                            _logger.LogDebug(
                                "Captured and cached HTML for route: {Route} ({Size} bytes, {Duration}s TTL)",
                                route,
                                capturedHtml.Length,
                                duration);
                        }
                        catch (Exception ex)
                        {
                            // Log error but don't prevent response from being sent
                            _logger.LogError(ex, "Failed to cache HTML for route: {Route}", context.Request.Path);
                        }
                    }
                }
            }

            // Copy the captured content back to the original response stream
            captureStream.Position = 0;
            await captureStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            // Always restore the original response body stream
            context.Response.Body = originalBodyStream;
        }
    }
}
