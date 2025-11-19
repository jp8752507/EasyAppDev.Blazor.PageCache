namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Represents the result of a content validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the validation error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets additional details about the validation failure.
    /// </summary>
    public Dictionary<string, string>? ErrorDetails { get; init; }

    /// <summary>
    /// Gets the validation severity level.
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="severity">The severity level (default: Error).</param>
    /// <param name="errorDetails">Additional error details.</param>
    public static ValidationResult Failure(
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error,
        Dictionary<string, string>? errorDetails = null) =>
        new()
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            Severity = severity,
            ErrorDetails = errorDetails
        };
}

/// <summary>
/// Defines the severity of a validation failure.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Warning - content can be cached but should be reviewed.
    /// </summary>
    Warning,

    /// <summary>
    /// Error - content should not be cached.
    /// </summary>
    Error,

    /// <summary>
    /// Critical - content is potentially malicious and should be rejected.
    /// </summary>
    Critical
}
