using EasyAppDev.Blazor.PageCache.Abstractions;
using Microsoft.Extensions.Logging;

namespace EasyAppDev.Blazor.PageCache.Events;

/// <summary>
/// Sample event handler that logs cache operations using structured logging.
/// </summary>
public sealed partial class LoggingPageCacheEvents : IPageCacheEvents
{
    private readonly ILogger<LoggingPageCacheEvents> _logger;

    public LoggingPageCacheEvents(ILogger<LoggingPageCacheEvents> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task OnCacheHitAsync(CacheHitContext context)
    {
        LogCacheHit(context.CacheKey, context.ContentSizeBytes, context.IsCompressed);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnCacheMissAsync(CacheMissContext context)
    {
        LogCacheMiss(context.CacheKey);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnCacheSetAsync(CacheSetContext context)
    {
        var compressionRatio = context.IsCompressed && context.OriginalSizeBytes > 0
            ? (double)context.ContentSizeBytes / context.OriginalSizeBytes
            : 1.0;

        LogCacheSet(
            context.CacheKey,
            context.ContentSizeBytes,
            context.DurationSeconds,
            context.IsCompressed,
            compressionRatio);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnCacheInvalidatedAsync(InvalidationContext context)
    {
        LogCacheInvalidated(context.KeyOrPattern, context.IsPattern, context.EntriesRemoved);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information,
        Message = "Cache hit: {CacheKey}, Size: {SizeBytes} bytes, Compressed: {IsCompressed}")]
    private partial void LogCacheHit(string cacheKey, long sizeBytes, bool isCompressed);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information,
        Message = "Cache miss: {CacheKey}")]
    private partial void LogCacheMiss(string cacheKey);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information,
        Message = "Cache set: {CacheKey}, Size: {SizeBytes} bytes, Duration: {DurationSeconds}s, Compressed: {IsCompressed}, Ratio: {CompressionRatio:F2}")]
    private partial void LogCacheSet(string cacheKey, long sizeBytes, int durationSeconds, bool isCompressed, double compressionRatio);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information,
        Message = "Cache invalidated: {KeyOrPattern}, IsPattern: {IsPattern}, Removed: {EntriesRemoved}")]
    private partial void LogCacheInvalidated(string keyOrPattern, bool isPattern, int entriesRemoved);
}
