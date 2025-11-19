namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Defines a contract for validating cached content before storage.
/// </summary>
/// <remarks>
/// Content validators help prevent cache poisoning, XSS attacks, and DoS vulnerabilities
/// by validating content before it is stored in the cache.
/// </remarks>
public interface IContentValidator
{
    /// <summary>
    /// Validates the content asynchronously.
    /// </summary>
    /// <param name="content">The content to validate.</param>
    /// <param name="cacheKey">The cache key for the content (for context).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation result indicating success or failure with reasons.</returns>
    Task<ValidationResult> ValidateAsync(string content, string cacheKey, CancellationToken cancellationToken = default);
}
