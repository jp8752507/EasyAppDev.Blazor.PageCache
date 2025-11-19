using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EasyAppDev.Blazor.PageCache.Configuration;
using EasyAppDev.Blazor.PageCache.Validation;
using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Tests.Validation;

[Trait("Category", TestCategories.Unit)]
public sealed class SizeLimitValidatorTests
{
    [Fact]
    public async Task ValidateAsync_EmptyContent_ReturnsSuccess()
    {
        // Arrange
        var options = Options.Create(new SecurityOptions());
        var validator = new SizeLimitValidator(options, NullLogger<SizeLimitValidator>.Instance);

        // Act
        var result = await validator.ValidateAsync("", "test-key");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_ContentWithinLimit_ReturnsSuccess()
    {
        // Arrange
        var options = Options.Create(new SecurityOptions
        {
            MaxEntrySizeBytes = 1024 * 1024 // 1 MB
        });
        var validator = new SizeLimitValidator(options, NullLogger<SizeLimitValidator>.Instance);
        var content = new string('a', 1000); // 1KB

        // Act
        var result = await validator.ValidateAsync(content, "test-key");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_ContentExceedsLimit_ReturnsFailure()
    {
        // Arrange
        var options = Options.Create(new SecurityOptions
        {
            MaxEntrySizeBytes = 100 // 100 bytes
        });
        var validator = new SizeLimitValidator(options, NullLogger<SizeLimitValidator>.Instance);
        var content = new string('a', 200); // 200 bytes

        // Act
        var result = await validator.ValidateAsync(content, "test-key");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Error, result.Severity);
        Assert.Contains("exceeds maximum allowed size", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_LargeContent_ReturnsWarning()
    {
        // Arrange
        var options = Options.Create(new SecurityOptions
        {
            MaxEntrySizeBytes = 1024 * 1024, // 1 MB max
            WarnOnLargeEntrySizeBytes = 500 // 500 bytes warning threshold
        });
        var validator = new SizeLimitValidator(options, NullLogger<SizeLimitValidator>.Instance);
        var content = new string('a', 600); // 600 bytes

        // Act
        var result = await validator.ValidateAsync(content, "test-key");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Contains("unusually large", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_NoLimitSet_AlwaysReturnsSuccess()
    {
        // Arrange
        var options = Options.Create(new SecurityOptions
        {
            MaxEntrySizeBytes = null,
            WarnOnLargeEntrySizeBytes = null
        });
        var validator = new SizeLimitValidator(options, NullLogger<SizeLimitValidator>.Instance);
        var content = new string('a', 10_000_000); // 10 MB

        // Act
        var result = await validator.ValidateAsync(content, "test-key");

        // Assert
        Assert.True(result.IsValid);
    }
}
