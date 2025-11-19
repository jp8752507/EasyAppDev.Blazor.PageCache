using FluentAssertions;
using Microsoft.Extensions.Options;
using EasyAppDev.Blazor.PageCache.Configuration;

namespace EasyAppDev.Blazor.PageCache.Tests.Configuration;

public class PageCacheOptionsValidatorTests
{
    private readonly PageCacheOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithValidOptions_ReturnsSuccess()
    {
        // Arrange
        var options = new PageCacheOptions();

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
        result.Succeeded.Should().BeTrue();
        result.Failed.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithInvalidDefaultDuration_ReturnsFail(int duration)
    {
        // Arrange
        var options = new PageCacheOptions { DefaultDurationSeconds = duration };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain(f => f.Contains(nameof(options.DefaultDurationSeconds)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Validate_WithInvalidMaxCacheSize_ReturnsFail(int size)
    {
        // Arrange
        var options = new PageCacheOptions { MaxCacheSizeMB = size };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(nameof(options.MaxCacheSizeMB)));
    }

    [Fact]
    public void Validate_WithNullMaxCacheSize_ReturnsSuccess()
    {
        // Arrange
        var options = new PageCacheOptions { MaxCacheSizeMB = null };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-30)]
    public void Validate_WithInvalidSlidingExpiration_ReturnsFail(int seconds)
    {
        // Arrange
        var options = new PageCacheOptions { SlidingExpirationSeconds = seconds };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(nameof(options.SlidingExpirationSeconds)));
    }

    [Fact]
    public void Validate_WithNullSlidingExpiration_ReturnsSuccess()
    {
        // Arrange
        var options = new PageCacheOptions { SlidingExpirationSeconds = null };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Validate_WithInvalidCacheKeyPrefix_ReturnsFail(string? prefix)
    {
        // Arrange
        var options = new PageCacheOptions { CacheKeyPrefix = prefix! };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(nameof(options.CacheKeyPrefix)));
    }

    [Theory]
    [InlineData("ValidPrefix:")]
    [InlineData("Cache-")]
    [InlineData("MyApp")]
    public void Validate_WithValidCacheKeyPrefix_ReturnsSuccess(string prefix)
    {
        // Arrange
        var options = new PageCacheOptions { CacheKeyPrefix = prefix };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Validate_WithInvalidMaxConcurrentCacheGenerations_ReturnsFail(int maxConcurrent)
    {
        // Arrange
        var options = new PageCacheOptions { MaxConcurrentCacheGenerations = maxConcurrent };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(nameof(options.MaxConcurrentCacheGenerations)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_WithInvalidCacheGenerationTimeout_ReturnsFail(int timeout)
    {
        // Arrange
        var options = new PageCacheOptions { CacheGenerationTimeoutSeconds = timeout };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(nameof(options.CacheGenerationTimeoutSeconds)));
    }

    [Fact]
    public void Validate_WithEmptyCacheableStatusCodes_ReturnsFail()
    {
        // Arrange
        var options = new PageCacheOptions { CacheableStatusCodes = new HashSet<int>() };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains(nameof(options.CacheableStatusCodes)));
    }

    [Fact]
    public void Validate_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var options = new PageCacheOptions
        {
            DefaultDurationSeconds = 0,
            MaxCacheSizeMB = -1,
            CacheKeyPrefix = "",
            MaxConcurrentCacheGenerations = 0,
            CacheableStatusCodes = new HashSet<int>()
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void Validate_WithNameParameter_ReturnsSuccess()
    {
        // Arrange
        var options = new PageCacheOptions();

        // Act
        var result = _validator.Validate("NamedOptions", options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }
}
