using System.Collections.Immutable;
using Xunit;
using EasyAppDev.Blazor.PageCache.Models;

namespace EasyAppDev.Blazor.PageCache.Tests.Models;

[Trait("Category", TestCategories.Unit)]
public sealed class CacheKeyTests
{
    [Fact]
    public void Constructor_WithRoute_CreatesKey()
    {
        // Arrange & Act
        var key = new CacheKey("/home");

        // Assert
        Assert.Equal("/home", key.Route);
        Assert.Empty(key.QueryParams);
        Assert.Null(key.Culture);
        Assert.Null(key.Header);
    }

    [Fact]
    public void ToString_SimpleRoute_ReturnsRoute()
    {
        // Arrange
        var key = new CacheKey("/home");

        // Act
        var result = key.ToString();

        // Assert
        Assert.Equal("/home", result);
    }

    [Fact]
    public void ToString_WithQueryParams_FormatsCorrectly()
    {
        // Arrange
        var queryParams = new Dictionary<string, string>
        {
            ["id"] = "123",
            ["name"] = "test"
        }.ToImmutableDictionary();

        var key = new CacheKey("/page", queryParams);

        // Act
        var result = key.ToString();

        // Assert
        Assert.Contains("/page?", result);
        Assert.Contains("id=123", result);
        Assert.Contains("name=test", result);
    }

    [Fact]
    public void ToString_WithCulture_IncludesCulture()
    {
        // Arrange
        var key = new CacheKey("/home", culture: "en-US");

        // Act
        var result = key.ToString();

        // Assert
        Assert.Equal("/home|culture=en-US", result);
    }

    [Fact]
    public void Parse_SimpleRoute_ParsesCorrectly()
    {
        // Arrange
        var keyString = "/home";

        // Act
        var key = CacheKey.Parse(keyString);

        // Assert
        Assert.Equal("/home", key.Route);
        Assert.Empty(key.QueryParams);
    }

    [Fact]
    public void Parse_WithQueryParams_ParsesCorrectly()
    {
        // Arrange
        var keyString = "/page?id=123&name=test";

        // Act
        var key = CacheKey.Parse(keyString);

        // Assert
        Assert.Equal("/page", key.Route);
        Assert.Equal(2, key.QueryParams.Count);
        Assert.Equal("123", key.QueryParams["id"]);
        Assert.Equal("test", key.QueryParams["name"]);
    }

    [Fact]
    public void Parse_WithCulture_ParsesCorrectly()
    {
        // Arrange
        var keyString = "/home|culture=en-US";

        // Act
        var key = CacheKey.Parse(keyString);

        // Assert
        Assert.Equal("/home", key.Route);
        Assert.Equal("en-US", key.Culture);
    }

    [Fact]
    public void Parse_ComplexKey_ParsesAllComponents()
    {
        // Arrange
        var keyString = "/page?id=123|culture=en-US|header=value";

        // Act
        var key = CacheKey.Parse(keyString);

        // Assert
        Assert.Equal("/page", key.Route);
        Assert.Equal("123", key.QueryParams["id"]);
        Assert.Equal("en-US", key.Culture);
        Assert.Equal("value", key.Header);
    }

    [Fact]
    public void TryParse_ValidKey_ReturnsTrue()
    {
        // Arrange
        var keyString = "/home";

        // Act
        var success = CacheKey.TryParse(keyString, out var key);

        // Assert
        Assert.True(success);
        Assert.Equal("/home", key.Route);
    }

    [Fact]
    public void TryParse_NullKey_ReturnsFalse()
    {
        // Act
        var success = CacheKey.TryParse(null, out var key);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void ImplicitConversion_ToString_Works()
    {
        // Arrange
        var key = new CacheKey("/home");

        // Act
        string keyString = key;

        // Assert
        Assert.Equal("/home", keyString);
    }

    [Fact]
    public void Builder_FluentAPI_BuildsCorrectKey()
    {
        // Act
        var key = CacheKey.Create("/page")
            .WithQueryParam("id", "123")
            .WithQueryParam("sort", "name")
            .WithCulture("en-US")
            .WithHeader("custom-header")
            .Build();

        // Assert
        Assert.Equal("/page", key.Route);
        Assert.Equal(2, key.QueryParams.Count);
        Assert.Equal("123", key.QueryParams["id"]);
        Assert.Equal("name", key.QueryParams["sort"]);
        Assert.Equal("en-US", key.Culture);
        Assert.Equal("custom-header", key.Header);
    }

    [Fact]
    public void Builder_ImplicitConversion_Works()
    {
        // Act
        CacheKey key = CacheKey.Create("/home")
            .WithCulture("en-US");

        // Assert
        Assert.Equal("/home", key.Route);
        Assert.Equal("en-US", key.Culture);
    }

    [Fact]
    public void ToString_EscapesSpecialCharacters()
    {
        // Arrange
        var queryParams = new Dictionary<string, string>
        {
            ["email"] = "test@example.com",
            ["name"] = "John Doe"
        }.ToImmutableDictionary();

        var key = new CacheKey("/page", queryParams);

        // Act
        var result = key.ToString();

        // Assert
        Assert.Contains("email=test%40example.com", result);
        Assert.Contains("name=John%20Doe", result);
    }

    [Fact]
    public void Parse_UnescapesSpecialCharacters()
    {
        // Arrange
        var keyString = "/page?email=test%40example.com&name=John%20Doe";

        // Act
        var key = CacheKey.Parse(keyString);

        // Assert
        Assert.Equal("test@example.com", key.QueryParams["email"]);
        Assert.Equal("John Doe", key.QueryParams["name"]);
    }

    [Fact]
    public void RoundTrip_PreservesKeyStructure()
    {
        // Arrange
        var originalKey = CacheKey.Create("/page")
            .WithQueryParam("id", "123")
            .WithQueryParam("filter", "active")
            .WithCulture("en-US")
            .WithHeader("custom")
            .WithMetadata("version", "2")
            .Build();

        // Act
        var keyString = originalKey.ToString();
        var parsedKey = CacheKey.Parse(keyString);

        // Assert
        Assert.Equal(originalKey.Route, parsedKey.Route);
        Assert.Equal(originalKey.QueryParams.Count, parsedKey.QueryParams.Count);
        Assert.Equal(originalKey.Culture, parsedKey.Culture);
        Assert.Equal(originalKey.Header, parsedKey.Header);
    }
}
