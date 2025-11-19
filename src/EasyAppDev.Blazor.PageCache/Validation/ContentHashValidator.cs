using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Validation;

/// <summary>
/// Validates content integrity by computing and optionally storing content hashes.
/// </summary>
public sealed partial class ContentHashValidator : IContentValidator
{
    private readonly ILogger<ContentHashValidator> _logger;
    private readonly bool _enableHashValidation;

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _hashStore = new();

    public ContentHashValidator(
        ILogger<ContentHashValidator> logger,
        bool enableHashValidation = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enableHashValidation = enableHashValidation;
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateAsync(
        string content,
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        if (!_enableHashValidation || string.IsNullOrEmpty(content))
        {
            return Task.FromResult(ValidationResult.Success());
        }

        var contentHash = ComputeHash(content);

        // Check if we have a previous hash for this key
        if (_hashStore.TryGetValue(cacheKey, out var existingHash))
        {
            if (existingHash != contentHash)
            {
                LogContentHashMismatch(cacheKey, existingHash, contentHash);

                return Task.FromResult(ValidationResult.Failure(
                    "Content hash mismatch detected - possible tampering",
                    ValidationSeverity.Critical,
                    new Dictionary<string, string>
                    {
                        ["ExpectedHash"] = existingHash,
                        ["ActualHash"] = contentHash
                    }));
            }
        }
        else
        {
            // Store the hash for future validation
            _hashStore[cacheKey] = contentHash;
            LogContentHashStored(cacheKey, contentHash);
        }

        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Computes the SHA256 hash of the content.
    /// </summary>
    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Removes the stored hash for a cache key.
    /// </summary>
    /// <param name="cacheKey">The cache key.</param>
    public void RemoveHash(string cacheKey)
    {
        _hashStore.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// Clears all stored hashes.
    /// </summary>
    public void ClearHashes()
    {
        _hashStore.Clear();
    }

    [LoggerMessage(EventId = 3020, Level = LogLevel.Warning,
        Message = "Content hash mismatch for cache key '{CacheKey}': Expected={ExpectedHash}, Actual={ActualHash}")]
    private partial void LogContentHashMismatch(string cacheKey, string expectedHash, string actualHash);

    [LoggerMessage(EventId = 3021, Level = LogLevel.Debug,
        Message = "Content hash stored for cache key '{CacheKey}': {Hash}")]
    private partial void LogContentHashStored(string cacheKey, string hash);
}
