namespace EasyAppDev.Blazor.PageCache.Configuration;

/// <summary>
/// Configuration options for the Blazor page cache.
/// </summary>
public sealed class PageCacheOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether page caching is enabled globally.
    /// Default is <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default cache duration in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    public int DefaultDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the maximum size of the cache in megabytes.
    /// Default is 100 MB. Set to null for no limit.
    /// </summary>
    public int? MaxCacheSizeMB { get; set; } = 100;

    /// <summary>
    /// Gets or sets the sliding expiration duration in seconds.
    /// Default is null (no sliding expiration).
    /// </summary>
    public int? SlidingExpirationSeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to compress cached HTML.
    /// Default is <c>false</c>.
    /// </summary>
    public bool CompressCachedContent { get; set; } = false;

    /// <summary>
    /// Gets or sets the type of compression strategy to use.
    /// Default is null (uses GZipCompressionStrategy when CompressCachedContent is true).
    /// </summary>
    public Type? CompressionStrategyType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable cache statistics tracking.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableStatistics { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache key prefix.
    /// Default is "PageCache:".
    /// </summary>
    public string CacheKeyPrefix { get; set; } = "PageCache:";

    /// <summary>
    /// Gets or sets a value indicating whether to vary cache by culture.
    /// Default is <c>true</c>.
    /// </summary>
    public bool VaryByCulture { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of concurrent cache generation operations
    /// per cache key. Default is 1 (prevents cache stampede).
    /// </summary>
    public int MaxConcurrentCacheGenerations { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timeout in seconds for waiting for a cache generation
    /// operation to complete. Default is 30 seconds.
    /// </summary>
    public int CacheGenerationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets query string parameters that should be ignored when
    /// generating cache keys (case-insensitive).
    /// </summary>
    public HashSet<string> IgnoredQueryParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "utm_source",
        "utm_medium",
        "utm_campaign",
        "utm_term",
        "utm_content",
        "fbclid",
        "gclid"
    };

    /// <summary>
    /// Gets or sets a value indicating whether to cache pages only for successful
    /// responses (HTTP 200). Default is <c>true</c>.
    /// </summary>
    public bool CacheOnlySuccessfulResponses { get; set; } = true;

    /// <summary>
    /// Gets or sets HTTP status codes that should be cached.
    /// Default is { 200 }. Only used if CacheOnlySuccessfulResponses is false.
    /// </summary>
    public HashSet<int> CacheableStatusCodes { get; set; } = new() { 200 };
}
