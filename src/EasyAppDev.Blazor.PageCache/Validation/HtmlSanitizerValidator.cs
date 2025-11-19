using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Configuration;

namespace EasyAppDev.Blazor.PageCache.Validation;

/// <summary>
/// Validates HTML content for potential XSS vulnerabilities and malicious patterns.
/// </summary>
public sealed partial class HtmlSanitizerValidator : IContentValidator
{
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<HtmlSanitizerValidator> _logger;

    private static readonly Regex[] SuspiciousPatterns = new[]
    {
        new Regex(@"on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"data:text/html[^,]*,.*<script", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"<script[^>]*>.*?(eval|document\.cookie|window\.location)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
        new Regex(@"<script[^>]*>.*?atob\(", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline),
        new Regex(@"expression\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    public HtmlSanitizerValidator(
        IOptions<SecurityOptions> securityOptions,
        ILogger<HtmlSanitizerValidator> logger)
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
        if (!_securityOptions.EnableHtmlValidation)
        {
            return Task.FromResult(ValidationResult.Success());
        }

        if (string.IsNullOrEmpty(content))
        {
            return Task.FromResult(ValidationResult.Success());
        }

        foreach (var pattern in SuspiciousPatterns)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var match = pattern.Match(content);
            if (match.Success)
            {
                LogSuspiciousContentDetected(cacheKey, match.Value);

                return Task.FromResult(ValidationResult.Failure(
                    "Potentially malicious content detected in HTML",
                    ValidationSeverity.Critical,
                    new Dictionary<string, string>
                    {
                        ["Pattern"] = pattern.ToString(),
                        ["MatchedContent"] = match.Value.Length > 100
                            ? match.Value.Substring(0, 100) + "..."
                            : match.Value
                    }));
            }
        }

        var scriptTagCount = Regex.Matches(content, @"<script[^>]*>", RegexOptions.IgnoreCase).Count;
        if (scriptTagCount > _securityOptions.MaxScriptTagsAllowed)
        {
            LogExcessiveScriptTags(cacheKey, scriptTagCount);

            return Task.FromResult(ValidationResult.Failure(
                $"Too many script tags detected ({scriptTagCount}), maximum allowed is {_securityOptions.MaxScriptTagsAllowed}",
                ValidationSeverity.Warning,
                new Dictionary<string, string>
                {
                    ["ScriptTagCount"] = scriptTagCount.ToString(),
                    ["MaxAllowed"] = _securityOptions.MaxScriptTagsAllowed.ToString()
                }));
        }

        return Task.FromResult(ValidationResult.Success());
    }

    [LoggerMessage(EventId = 3010, Level = LogLevel.Warning,
        Message = "Suspicious content detected in cache key '{CacheKey}': {MatchedContent}")]
    private partial void LogSuspiciousContentDetected(string cacheKey, string matchedContent);

    [LoggerMessage(EventId = 3011, Level = LogLevel.Information,
        Message = "Excessive script tags detected in cache key '{CacheKey}': {Count} tags")]
    private partial void LogExcessiveScriptTags(string cacheKey, int count);
}
