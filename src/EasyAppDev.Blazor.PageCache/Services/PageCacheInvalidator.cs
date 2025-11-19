using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using EasyAppDev.Blazor.PageCache.Configuration;

namespace EasyAppDev.Blazor.PageCache.Services;

/// <summary>
/// Implementation of <see cref="IPageCacheInvalidator"/>.
/// </summary>
public sealed partial class PageCacheInvalidator : IPageCacheInvalidator
{
    private readonly IPageCacheService _cacheService;
    private readonly PageCacheOptions _options;
    private readonly ILogger<PageCacheInvalidator> _logger;

    private readonly ConcurrentDictionary<string, HashSet<string>> _routeToCacheKeys = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _tagToCacheKeys = new();

    public PageCacheInvalidator(
        IPageCacheService cacheService,
        IOptions<PageCacheOptions> options,
        ILogger<PageCacheInvalidator> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool InvalidateRoute(string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);

        var normalizedRoute = NormalizeRoute(route);
        var pattern = $"{_options.CacheKeyPrefix}{normalizedRoute}*";

        var removed = _cacheService.RemoveByPattern(pattern);

        if (removed > 0)
        {
            _routeToCacheKeys.TryRemove(normalizedRoute, out _);
            LogRouteInvalidated(normalizedRoute, removed);
            return true;
        }

        LogRouteNotFound(normalizedRoute);
        return false;
    }

    /// <inheritdoc />
    public int InvalidatePattern(string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var normalizedPattern = NormalizeRoute(pattern);
        var cachePattern = $"{_options.CacheKeyPrefix}{normalizedPattern}";

        var removed = _cacheService.RemoveByPattern(cachePattern);

        if (removed > 0)
        {
            var routesToRemove = _routeToCacheKeys.Keys
                .Where(route => PatternMatches(route, normalizedPattern))
                .ToList();

            foreach (var route in routesToRemove)
            {
                _routeToCacheKeys.TryRemove(route, out _);
            }
        }

        LogPatternInvalidated(pattern, removed);
        return removed;
    }

    /// <inheritdoc />
    public int InvalidateByTag(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        if (!_tagToCacheKeys.TryGetValue(tag, out var cacheKeys))
        {
            LogTagNotFound(tag);
            return 0;
        }

        var removed = 0;
        var keysSnapshot = cacheKeys.ToList(); // Create a snapshot to avoid collection modification issues

        foreach (var cacheKey in keysSnapshot)
        {
            _cacheService.Remove(cacheKey);
            removed++;
        }

        _tagToCacheKeys.TryRemove(tag, out _);

        LogTagInvalidated(tag, removed);
        return removed;
    }

    /// <inheritdoc />
    public int ClearAll()
    {
        _cacheService.Clear();

        var count = _routeToCacheKeys.Count;
        _routeToCacheKeys.Clear();
        _tagToCacheKeys.Clear();

        LogCacheCleared(count);
        return count;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetCachedRoutes()
    {
        return _routeToCacheKeys.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Registers a cache key for route tracking.
    /// </summary>
    /// <param name="route">The route being cached.</param>
    /// <param name="cacheKey">The cache key used for storage.</param>
    /// <param name="tags">Optional tags for grouping cache entries.</param>
    /// <remarks>
    /// Called internally by PageCacheService when caching a page.
    /// </remarks>
    internal void RegisterCacheKey(string route, string cacheKey, string[]? tags = null)
    {
        var normalizedRoute = NormalizeRoute(route);

        _routeToCacheKeys.AddOrUpdate(
            normalizedRoute,
            _ => new HashSet<string> { cacheKey },
            (_, existing) =>
            {
                lock (existing) // Thread-safe HashSet modification
                {
                    existing.Add(cacheKey);
                }
                return existing;
            });

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                _tagToCacheKeys.AddOrUpdate(
                    tag,
                    _ => new HashSet<string> { cacheKey },
                    (_, existing) =>
                    {
                        lock (existing) // Thread-safe HashSet modification
                        {
                            existing.Add(cacheKey);
                        }
                        return existing;
                    });
            }
        }
    }

    /// <summary>
    /// Normalizes a route for consistent matching.
    /// </summary>
    private static string NormalizeRoute(string route)
    {
        var normalized = route.Trim().ToLowerInvariant();

        // Ensure starts with / unless it's a wildcard pattern
        if (!normalized.StartsWith('/') && !normalized.StartsWith('*'))
        {
            normalized = "/" + normalized;
        }

        // Remove trailing slash except for root
        if (normalized.Length > 1 && normalized.EndsWith('/') && !normalized.EndsWith("*/"))
        {
            normalized = normalized[..^1];
        }

        return normalized;
    }

    /// <summary>
    /// Checks if a route matches a pattern.
    /// </summary>
    private static bool PatternMatches(string route, string pattern)
    {
        // If no wildcard, just do exact comparison
        if (!pattern.Contains('*'))
        {
            return route.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        try
        {
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*") + "$";

            return Regex.IsMatch(
                route,
                regexPattern,
                RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException)
        {
            // If regex times out, assume no match
            return false;
        }
    }

    // Source-generated logging methods
    [LoggerMessage(EventId = 4001, Level = LogLevel.Information,
        Message = "Invalidated route: {Route}, {Count} entries removed")]
    private partial void LogRouteInvalidated(string route, int count);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Debug,
        Message = "Route not found in cache: {Route}")]
    private partial void LogRouteNotFound(string route);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Information,
        Message = "Invalidated pattern: {Pattern}, {Count} entries removed")]
    private partial void LogPatternInvalidated(string pattern, int count);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Information,
        Message = "Invalidated tag: {Tag}, {Count} entries removed")]
    private partial void LogTagInvalidated(string tag, int count);

    [LoggerMessage(EventId = 4005, Level = LogLevel.Debug,
        Message = "Tag not found in cache: {Tag}")]
    private partial void LogTagNotFound(string tag);

    [LoggerMessage(EventId = 4006, Level = LogLevel.Warning,
        Message = "Cache cleared: {Count} routes removed")]
    private partial void LogCacheCleared(int count);
}
