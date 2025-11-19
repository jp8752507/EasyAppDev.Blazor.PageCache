using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Configuration;

namespace EasyAppDev.Blazor.PageCache.Validation;

/// <summary>
/// Validates that cached content does not exceed configured size limits.
/// </summary>
public sealed partial class SizeLimitValidator : IContentValidator
{
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<SizeLimitValidator> _logger;

    public SizeLimitValidator(
        IOptions<SecurityOptions> securityOptions,
        ILogger<SizeLimitValidator> logger)
    {
        _securityOptions = securityOptions?.Value ?? throw new ArgumentNullException(nameof(securityOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateAsync(
        string content,
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(content))
        {
            return Task.FromResult(ValidationResult.Success());
        }

        var contentSizeBytes = System.Text.Encoding.UTF8.GetByteCount(content);

        if (_securityOptions.MaxEntrySizeBytes.HasValue &&
            contentSizeBytes > _securityOptions.MaxEntrySizeBytes.Value)
        {
            LogSizeLimitExceeded(cacheKey, contentSizeBytes, _securityOptions.MaxEntrySizeBytes.Value);

            return Task.FromResult(ValidationResult.Failure(
                $"Content size ({contentSizeBytes} bytes) exceeds maximum allowed size ({_securityOptions.MaxEntrySizeBytes.Value} bytes)",
                ValidationSeverity.Error,
                new Dictionary<string, string>
                {
                    ["ContentSizeBytes"] = contentSizeBytes.ToString(),
                    ["MaxSizeBytes"] = _securityOptions.MaxEntrySizeBytes.Value.ToString()
                }));
        }

        // Warn if content is large
        if (_securityOptions.WarnOnLargeEntrySizeBytes.HasValue &&
            contentSizeBytes > _securityOptions.WarnOnLargeEntrySizeBytes.Value)
        {
            LogLargeContentWarning(cacheKey, contentSizeBytes);

            return Task.FromResult(ValidationResult.Failure(
                $"Content size ({contentSizeBytes} bytes) is unusually large",
                ValidationSeverity.Warning,
                new Dictionary<string, string>
                {
                    ["ContentSizeBytes"] = contentSizeBytes.ToString(),
                    ["WarnThresholdBytes"] = _securityOptions.WarnOnLargeEntrySizeBytes.Value.ToString()
                }));
        }

        return Task.FromResult(ValidationResult.Success());
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning,
        Message = "Content for cache key '{CacheKey}' exceeds size limit: {ActualSize} bytes > {MaxSize} bytes")]
    private partial void LogSizeLimitExceeded(string cacheKey, int actualSize, int maxSize);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information,
        Message = "Large content detected for cache key '{CacheKey}': {Size} bytes")]
    private partial void LogLargeContentWarning(string cacheKey, int size);
}
