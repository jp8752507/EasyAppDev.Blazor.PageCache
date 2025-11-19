using Microsoft.Extensions.Options;

namespace EasyAppDev.Blazor.PageCache.Configuration;

/// <summary>
/// Validates <see cref="PageCacheOptions"/> configuration.
/// </summary>
internal sealed class PageCacheOptionsValidator : IValidateOptions<PageCacheOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PageCacheOptions options)
    {
        var errors = new List<string>();

        if (options.DefaultDurationSeconds <= 0)
        {
            errors.Add($"{nameof(options.DefaultDurationSeconds)} must be greater than 0.");
        }

        if (options.MaxCacheSizeMB.HasValue && options.MaxCacheSizeMB.Value <= 0)
        {
            errors.Add($"{nameof(options.MaxCacheSizeMB)} must be greater than 0 or null.");
        }

        if (options.SlidingExpirationSeconds.HasValue && options.SlidingExpirationSeconds.Value <= 0)
        {
            errors.Add($"{nameof(options.SlidingExpirationSeconds)} must be greater than 0 or null.");
        }

        if (string.IsNullOrWhiteSpace(options.CacheKeyPrefix))
        {
            errors.Add($"{nameof(options.CacheKeyPrefix)} cannot be null or whitespace.");
        }

        if (options.MaxConcurrentCacheGenerations <= 0)
        {
            errors.Add($"{nameof(options.MaxConcurrentCacheGenerations)} must be greater than 0.");
        }

        if (options.CacheGenerationTimeoutSeconds <= 0)
        {
            errors.Add($"{nameof(options.CacheGenerationTimeoutSeconds)} must be greater than 0.");
        }

        if (options.CacheableStatusCodes.Count == 0)
        {
            errors.Add($"{nameof(options.CacheableStatusCodes)} must contain at least one status code.");
        }

        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }
}
