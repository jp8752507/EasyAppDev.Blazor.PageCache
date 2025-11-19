using Microsoft.Extensions.Logging;
using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Validation;

/// <summary>
/// Combines multiple content validators into a single validation pipeline.
/// </summary>
public sealed partial class CompositeContentValidator : IContentValidator
{
    private readonly IEnumerable<IContentValidator> _validators;
    private readonly ILogger<CompositeContentValidator> _logger;

    public CompositeContentValidator(
        IEnumerable<IContentValidator> validators,
        ILogger<CompositeContentValidator> logger)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync(
        string content,
        string cacheKey,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();

        foreach (var validator in _validators)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValidationResult.Failure("Validation cancelled", ValidationSeverity.Error);
            }

            try
            {
                var result = await validator.ValidateAsync(content, cacheKey, cancellationToken);

                if (!result.IsValid)
                {
                    // Stop on errors and critical issues
                    if (result.Severity >= ValidationSeverity.Error)
                    {
                        LogValidationFailed(cacheKey, validator.GetType().Name, result.ErrorMessage ?? "Unknown error");
                        return result;
                    }

                    // Collect warnings but continue
                    if (result.Severity == ValidationSeverity.Warning)
                    {
                        warnings.Add($"{validator.GetType().Name}: {result.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogValidatorException(cacheKey, validator.GetType().Name, ex);

                return ValidationResult.Failure(
                    $"Validator {validator.GetType().Name} threw an exception: {ex.Message}",
                    ValidationSeverity.Error);
            }
        }

        // If we collected warnings, return a warning result
        if (warnings.Count > 0)
        {
            return ValidationResult.Failure(
                string.Join("; ", warnings),
                ValidationSeverity.Warning);
        }

        return ValidationResult.Success();
    }

    [LoggerMessage(EventId = 3030, Level = LogLevel.Warning,
        Message = "Validation failed for cache key '{CacheKey}' by validator '{ValidatorName}': {Error}")]
    private partial void LogValidationFailed(string cacheKey, string validatorName, string error);

    [LoggerMessage(EventId = 3031, Level = LogLevel.Error,
        Message = "Validator '{ValidatorName}' threw exception for cache key '{CacheKey}'")]
    private partial void LogValidatorException(string cacheKey, string validatorName, Exception exception);
}
