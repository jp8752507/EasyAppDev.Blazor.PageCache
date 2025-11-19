namespace EasyAppDev.Blazor.PageCache.Configuration;

/// <summary>
/// Security-related configuration options for page caching.
/// </summary>
public sealed class SecurityOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether HTML validation is enabled.
    /// Default is <c>false</c>.
    /// </summary>
    /// <remarks>
    /// When enabled, cached HTML will be scanned for potentially malicious patterns
    /// like inline scripts, event handlers, and javascript: URLs.
    /// </remarks>
    public bool EnableHtmlValidation { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether content size validation is enabled.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableSizeValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum allowed size per cache entry in bytes.
    /// Default is 5 MB (5,242,880 bytes). Set to null for no limit.
    /// </summary>
    /// <remarks>
    /// This helps prevent DoS attacks where an attacker tries to exhaust
    /// cache memory by causing the server to cache very large responses.
    /// </remarks>
    public int? MaxEntrySizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Gets or sets the threshold for warning about large cache entries in bytes.
    /// Default is 1 MB (1,048,576 bytes). Set to null to disable warnings.
    /// </summary>
    public int? WarnOnLargeEntrySizeBytes { get; set; } = 1024 * 1024; // 1 MB

    /// <summary>
    /// Gets or sets the maximum number of script tags allowed in cached content.
    /// Default is 50.
    /// </summary>
    /// <remarks>
    /// An unusually high number of script tags may indicate compromised content.
    /// This check only applies when EnableHtmlValidation is true.
    /// </remarks>
    public int MaxScriptTagsAllowed { get; set; } = 50;

    /// <summary>
    /// Gets or sets a value indicating whether to enable rate limiting per cache key.
    /// Default is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Prevents abuse where an attacker repeatedly requests cache regeneration
    /// to cause resource exhaustion.
    /// </remarks>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of cache regeneration attempts per key within the time window.
    /// Default is 10.
    /// </summary>
    public int RateLimitMaxAttempts { get; set; } = 10;

    /// <summary>
    /// Gets or sets the rate limit time window in seconds.
    /// Default is 60 seconds.
    /// </summary>
    public int RateLimitWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets a value indicating whether to cache content for authenticated users.
    /// Default is <c>false</c> for security reasons.
    /// </summary>
    /// <remarks>
    /// SECURITY WARNING: Caching authenticated user content can lead to data leakage
    /// if not configured properly. Only enable this if you understand the risks and
    /// have proper cache key generation that includes user identity.
    /// </remarks>
    public bool CacheForAuthenticatedUsers { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to log security validation failures.
    /// Default is <c>true</c>.
    /// </summary>
    public bool LogSecurityEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to block caching on validation failure.
    /// Default is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// When true, content that fails validation will not be cached.
    /// When false, validation warnings are logged but caching proceeds.
    /// Critical validation failures always block caching regardless of this setting.
    /// </remarks>
    public bool BlockOnValidationFailure { get; set; } = true;
}
