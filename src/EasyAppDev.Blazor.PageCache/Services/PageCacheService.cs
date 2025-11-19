using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Configuration;
using EasyAppDev.Blazor.PageCache.Diagnostics;
using EasyAppDev.Blazor.PageCache.Storage;

namespace EasyAppDev.Blazor.PageCache.Services;

/// <summary>
/// Implementation of <see cref="IPageCacheService"/> using pluggable storage backends.
/// </summary>
public sealed partial class PageCacheService : IPageCacheService, IDisposable
{
    private readonly ICacheStorage _storage;
    private readonly PageCacheOptions _options;
    private readonly AsyncKeyedLock _locks;
    private readonly ILogger<PageCacheService> _logger;
    private readonly ICompressionStrategy? _compressionStrategy;
    private readonly IPageCacheEvents _events;
    private readonly IContentValidator? _contentValidator;

    // Statistics tracking
    private long _hitCount;
    private long _missCount;
    private long _totalCachedBytes;
    private long _evictionCount;
    private readonly DateTimeOffset _startTime = DateTimeOffset.UtcNow;

    public PageCacheService(
        ICacheStorage storage,
        IOptions<PageCacheOptions> options,
        AsyncKeyedLock locks,
        ILogger<PageCacheService> logger,
        IPageCacheEvents events,
        ICompressionStrategy? compressionStrategy = null,
        IContentValidator? contentValidator = null)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _locks = locks ?? throw new ArgumentNullException(nameof(locks));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _compressionStrategy = compressionStrategy;
        _contentValidator = contentValidator;
    }

    /// <inheritdoc />
    public string? GetCachedHtml(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        if (!_options.Enabled)
        {
            return null;
        }

        if (_compressionStrategy != null)
        {
            var compressedData = _storage.GetAsync<byte[]>(cacheKey).AsTask().GetAwaiter().GetResult();
            if (compressedData != null)
            {
                if (_options.EnableStatistics)
                {
                    Interlocked.Increment(ref _hitCount);
                }

                LogCacheHit(cacheKey);

                _ = _events.OnCacheHitAsync(new CacheHitContext
                {
                    CacheKey = cacheKey,
                    ContentSizeBytes = compressedData.Length,
                    IsCompressed = true
                });

                return _compressionStrategy.Decompress(compressedData);
            }
        }
        else
        {
            var html = _storage.GetAsync<string>(cacheKey).AsTask().GetAwaiter().GetResult();
            if (html != null)
            {
                if (_options.EnableStatistics)
                {
                    Interlocked.Increment(ref _hitCount);
                }

                LogCacheHit(cacheKey);

                _ = _events.OnCacheHitAsync(new CacheHitContext
                {
                    CacheKey = cacheKey,
                    ContentSizeBytes = html.Length,
                    IsCompressed = false
                });

                return html;
            }
        }

        if (_options.EnableStatistics)
        {
            Interlocked.Increment(ref _missCount);
        }

        LogCacheMiss(cacheKey);

        _ = _events.OnCacheMissAsync(new CacheMissContext
        {
            CacheKey = cacheKey
        });

        return null;
    }

    /// <inheritdoc />
    public async Task SetCachedHtmlAsync(string cacheKey, string html, int durationSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(html);

        if (!_options.Enabled)
        {
            return;
        }

        if (durationSeconds <= 0)
        {
            durationSeconds = _options.DefaultDurationSeconds;
        }

        // Validate content if validator is configured
        if (_contentValidator != null)
        {
            var validationResult = await _contentValidator.ValidateAsync(html, cacheKey);

            if (!validationResult.IsValid)
            {
                LogContentValidationFailed(cacheKey, validationResult.Severity.ToString(), validationResult.ErrorMessage ?? "Unknown error");

                if (validationResult.Severity >= ValidationSeverity.Error)
                {
                    return;
                }
            }
        }

        long sizeBytes;
        object cacheValue;

        if (_compressionStrategy != null)
        {
            var compressedData = _compressionStrategy.Compress(html);
            cacheValue = compressedData;
            sizeBytes = compressedData.Length;
        }
        else
        {
            cacheValue = html;
            sizeBytes = html.Length;
        }

        var entryOptions = new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(durationSeconds),
            Size = sizeBytes
        };

        if (_options.SlidingExpirationSeconds.HasValue)
        {
            entryOptions.SlidingExpiration = TimeSpan.FromSeconds(_options.SlidingExpirationSeconds.Value);
        }

        entryOptions.PostEvictionCallback = (key, value, reason) =>
        {
            if (_options.EnableStatistics)
            {
                long size = value switch
                {
                    string str => str.Length,
                    byte[] bytes => bytes.Length,
                    _ => 0
                };
                Interlocked.Add(ref _totalCachedBytes, -size);
                Interlocked.Increment(ref _evictionCount);
            }

            LogCacheEvicted(key, reason.ToString());
        };

        if (_compressionStrategy != null)
        {
            await _storage.SetAsync(cacheKey, (byte[])cacheValue, entryOptions);
        }
        else
        {
            await _storage.SetAsync(cacheKey, (string)cacheValue, entryOptions);
        }

        if (_options.EnableStatistics)
        {
            Interlocked.Add(ref _totalCachedBytes, sizeBytes);
        }

        LogCacheSet(cacheKey, (int)sizeBytes, durationSeconds);

        _ = _events.OnCacheSetAsync(new CacheSetContext
        {
            CacheKey = cacheKey,
            ContentSizeBytes = sizeBytes,
            OriginalSizeBytes = html.Length,
            IsCompressed = _compressionStrategy != null,
            DurationSeconds = durationSeconds
        });
    }

    /// <inheritdoc />
    public void Remove(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        _storage.RemoveAsync(cacheKey).AsTask().GetAwaiter().GetResult();

        LogCacheRemoved(cacheKey);

        _ = _events.OnCacheInvalidatedAsync(new InvalidationContext
        {
            KeyOrPattern = cacheKey,
            IsPattern = false,
            EntriesRemoved = 1
        });
    }

    /// <inheritdoc />
    public int RemoveByPattern(string pattern, int maxRemovalCount = 10000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var removed = _storage.RemoveByPatternAsync(pattern, maxRemovalCount).AsTask().GetAwaiter().GetResult();

        LogPatternRemoved(pattern, removed);

        _ = _events.OnCacheInvalidatedAsync(new InvalidationContext
        {
            KeyOrPattern = pattern,
            IsPattern = true,
            EntriesRemoved = removed
        });

        return removed;
    }

    /// <inheritdoc />
    public void Clear()
    {
        var storage = _storage as MemoryCacheStorage;
        var count = storage?.Count ?? 0;

        _storage.ClearAsync().AsTask().GetAwaiter().GetResult();

        if (_options.EnableStatistics)
        {
            Interlocked.Exchange(ref _totalCachedBytes, 0);
        }

        LogCacheCleared(count);
    }

    /// <inheritdoc />
    public async Task<IDisposable> AcquireLockAsync(
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        var timeout = TimeSpan.FromSeconds(_options.CacheGenerationTimeoutSeconds);

        return await _locks.LockAsync(cacheKey, timeout, cancellationToken);
    }

    /// <summary>
    /// Gets current cache statistics.
    /// </summary>
    /// <returns>A <see cref="PageCacheStats"/> instance containing current statistics.</returns>
    public PageCacheStats GetStatistics()
    {
        if (!_options.EnableStatistics)
        {
            return new PageCacheStats
            {
                StartTime = _startTime
            };
        }

        var hits = Interlocked.Read(ref _hitCount);
        var misses = Interlocked.Read(ref _missCount);
        var total = hits + misses;
        var hitRate = total > 0 ? (double)hits / total : 0;

        var storage = _storage as MemoryCacheStorage;
        var cachedEntries = storage?.Count ?? 0;

        return new PageCacheStats
        {
            HitCount = hits,
            MissCount = misses,
            TotalRequests = total,
            HitRate = hitRate,
            CachedEntries = cachedEntries,
            CacheSizeBytes = Interlocked.Read(ref _totalCachedBytes),
            EvictionCount = Interlocked.Read(ref _evictionCount),
            StartTime = _startTime
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _locks.Dispose();
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Debug, Message = "Cache hit: {CacheKey}")]
    private partial void LogCacheHit(string cacheKey);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Debug, Message = "Cache miss: {CacheKey}")]
    private partial void LogCacheMiss(string cacheKey);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Debug,
        Message = "Cache set: {CacheKey}, Size: {SizeBytes} bytes, Duration: {DurationSeconds}s")]
    private partial void LogCacheSet(string cacheKey, int sizeBytes, int durationSeconds);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Debug, Message = "Cache removed: {CacheKey}")]
    private partial void LogCacheRemoved(string cacheKey);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information,
        Message = "Cache pattern removed: {Pattern}, Count: {Count}")]
    private partial void LogPatternRemoved(string pattern, int count);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Information,
        Message = "Cache cleared: {Count} entries removed")]
    private partial void LogCacheCleared(int count);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Debug,
        Message = "Cache evicted: {CacheKey}, Reason: {Reason}")]
    private partial void LogCacheEvicted(string cacheKey, string reason);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Warning,
        Message = "Content validation failed for cache key '{CacheKey}': Severity={Severity}, Error={Error}")]
    private partial void LogContentValidationFailed(string cacheKey, string severity, string error);
}
