using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using EasyAppDev.Blazor.PageCache.Configuration;
using EasyAppDev.Blazor.PageCache.Validation;
using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Tests.Validation;

[Trait("Category", TestCategories.Unit)]
public sealed class HtmlSanitizerValidatorTests
{
    [Fact]
    public async Task ValidateAsync_SafeHtml_ReturnsSuccess()
    {
        // Arrange
        var options = Options.Create(new SecurityOptions { EnableHtmlValidation = true });
        var validator = new HtmlSanitizerValidator(options, NullLogger<HtmlSanitizerValidator>.Instance);
        var content = "<div><p>Hello World</p></div>";

        // Act
        var result = await validator.ValidateAsync(content, "test-key");

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_InlineEventHandler_ReturnsFailure()
    {
        // Arrange
        var options = Options.Create(new SecurityOptions { EnableHtmlValidation = true });
        var validator = new HtmlSanitizerValidator(options, NullLogger<HtmlSanitizerValidator>.Instance);
        var content = "<button onclick='alert(1)'>Click me</button>";

        // Act
        var result = await validator.ValidateAsync(content, "test-key");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Critical, result.Severity);
        Assert.Contains("malicious content", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_JavaScriptProtocol_ReturnsFailure()
    {
        // Arrange
        var options = Options.Create(new SecurityOptions { EnableHtmlValidation = true });
        var validator = new HtmlSanitizerValidator(options, NullLogger<HtmlSanitizerValidator>.Instance);
        var content = "<a href='javascript:void(0)'>Link</a>";

        // Act
        var result = await validator.ValidateAsync(content, "test-key");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Critical, result.Severity);
    }

    [Fact]
    public async Task ValidateAsync_ExcessiveScriptTags_ReturnsWarning()
    {
        // Arrange
        var options = Options.Create(new SecurityOptions
        {
            EnableHtmlValidation = true,
            MaxScriptTagsAllowed = 2
        });
        var validator = new HtmlSanitizerValidator(options, NullLogger<HtmlSanitizerValidator>.Instance);
        var content = "<script>console.log(1)</script><script>console.log(2)</script><script>console.log(3)</script>";

        // Act
        var result = await validator.ValidateAsync(content, "test-key");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Contains("Too many script tags", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_ValidationDisabled_AlwaysReturnsSuccess()
    {
        // Arrange
        var options = Options.Create(new SecurityOptions { EnableHtmlValidation = false });
        var validator = new HtmlSanitizerValidator(options, NullLogger<HtmlSanitizerValidator>.Instance);
        var content = "<script>alert('XSS')</script>";

        // Act
        var result = await validator.ValidateAsync(content, "test-key");

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("<img src=x onerror='alert(1)'>")]
    [InlineData("<div onload='malicious()'>")]
    [InlineData("<a href='javascript:alert(1)'>")]
    public async Task ValidateAsync_CommonXssPatterns_ReturnsFailure(string maliciousContent)
    {
        // Arrange
        var options = Options.Create(new SecurityOptions { EnableHtmlValidation = true });
        var validator = new HtmlSanitizerValidator(options, NullLogger<HtmlSanitizerValidator>.Instance);

        // Act
        var result = await validator.ValidateAsync(maliciousContent, "test-key");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(ValidationSeverity.Critical, result.Severity);
    }
}
