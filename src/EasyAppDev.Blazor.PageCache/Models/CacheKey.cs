using System.Collections.Immutable;
using System.Text;

namespace EasyAppDev.Blazor.PageCache.Models;

/// <summary>
/// Represents a structured, type-safe cache key.
/// </summary>
public readonly record struct CacheKey
{
    public string Route { get; init; }

    public ImmutableDictionary<string, string> QueryParams { get; init; }

    public string? Culture { get; init; }

    public string? Header { get; init; }

    public ImmutableDictionary<string, string>? Metadata { get; init; }

    public CacheKey(
        string route,
        ImmutableDictionary<string, string>? queryParams = null,
        string? culture = null,
        string? header = null,
        ImmutableDictionary<string, string>? metadata = null)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route));
        QueryParams = queryParams ?? ImmutableDictionary<string, string>.Empty;
        Culture = culture;
        Header = header;
        Metadata = metadata;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Route);

        if (QueryParams.Count > 0)
        {
            sb.Append('?');
            sb.Append(string.Join("&", QueryParams
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}")));
        }

        // Add culture
        if (!string.IsNullOrEmpty(Culture))
        {
            sb.Append($"|culture={Culture}");
        }

        // Add header
        if (!string.IsNullOrEmpty(Header))
        {
            sb.Append($"|header={Header}");
        }

        // Add metadata
        if (Metadata != null && Metadata.Count > 0)
        {
            sb.Append('|');
            sb.Append(string.Join("|", Metadata
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Implicitly converts a CacheKey to a string.
    /// </summary>
    public static implicit operator string(CacheKey key) => key.ToString();

    /// <summary>
    /// Parses a cache key string into a structured CacheKey.
    /// </summary>
    /// <param name="keyString">The cache key string to parse.</param>
    /// <returns>The parsed CacheKey.</returns>
    /// <exception cref="FormatException">Thrown if the key string format is invalid.</exception>
    public static CacheKey Parse(string keyString)
    {
        if (string.IsNullOrWhiteSpace(keyString))
        {
            throw new ArgumentException("Cache key cannot be null or whitespace", nameof(keyString));
        }

        // Split by pipe to separate main parts from metadata
        var parts = keyString.Split('|');
        var mainPart = parts[0];

        // Parse route and query string
        var questionMarkIndex = mainPart.IndexOf('?');
        string route;
        ImmutableDictionary<string, string> queryParams;

        if (questionMarkIndex >= 0)
        {
            route = mainPart.Substring(0, questionMarkIndex);
            var queryString = mainPart.Substring(questionMarkIndex + 1);
            queryParams = ParseQueryString(queryString);
        }
        else
        {
            route = mainPart;
            queryParams = ImmutableDictionary<string, string>.Empty;
        }

        // Parse metadata parts
        string? culture = null;
        string? header = null;
        var metadata = ImmutableDictionary.CreateBuilder<string, string>();

        for (int i = 1; i < parts.Length; i++)
        {
            var part = parts[i];
            var equalIndex = part.IndexOf('=');

            if (equalIndex > 0)
            {
                var key = part.Substring(0, equalIndex);
                var value = part.Substring(equalIndex + 1);

                if (key.Equals("culture", StringComparison.OrdinalIgnoreCase))
                {
                    culture = value;
                }
                else if (key.Equals("header", StringComparison.OrdinalIgnoreCase))
                {
                    header = value;
                }
                else
                {
                    metadata[key] = value;
                }
            }
        }

        return new CacheKey(
            route,
            queryParams,
            culture,
            header,
            metadata.Count > 0 ? metadata.ToImmutable() : null);
    }

    /// <summary>
    /// Tries to parse a cache key string.
    /// </summary>
    /// <param name="keyString">The cache key string to parse.</param>
    /// <param name="cacheKey">The parsed CacheKey if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? keyString, out CacheKey cacheKey)
    {
        cacheKey = default;

        if (string.IsNullOrWhiteSpace(keyString))
        {
            return false;
        }

        try
        {
            cacheKey = Parse(keyString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ImmutableDictionary<string, string> ParseQueryString(string queryString)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>();

        if (string.IsNullOrEmpty(queryString))
        {
            return builder.ToImmutable();
        }

        var pairs = queryString.Split('&');
        foreach (var pair in pairs)
        {
            var equalIndex = pair.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = Uri.UnescapeDataString(pair.Substring(0, equalIndex));
                var value = Uri.UnescapeDataString(pair.Substring(equalIndex + 1));
                builder[key] = value;
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Creates a cache key builder for fluent construction.
    /// </summary>
    /// <param name="route">The route/path.</param>
    /// <returns>A new CacheKeyBuilder instance.</returns>
    public static CacheKeyBuilder Create(string route) => new(route);
}

/// <summary>
/// Builder for constructing CacheKey instances fluently.
/// </summary>
public sealed class CacheKeyBuilder
{
    private readonly string _route;
    private readonly Dictionary<string, string> _queryParams = new();
    private string? _culture;
    private string? _header;
    private readonly Dictionary<string, string> _metadata = new();

    internal CacheKeyBuilder(string route)
    {
        _route = route ?? throw new ArgumentNullException(nameof(route));
    }

    /// <summary>
    /// Adds a query parameter.
    /// </summary>
    public CacheKeyBuilder WithQueryParam(string key, string value)
    {
        _queryParams[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple query parameters.
    /// </summary>
    public CacheKeyBuilder WithQueryParams(IEnumerable<KeyValuePair<string, string>> queryParams)
    {
        foreach (var kvp in queryParams)
        {
            _queryParams[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Sets the culture.
    /// </summary>
    public CacheKeyBuilder WithCulture(string? culture)
    {
        _culture = culture;
        return this;
    }

    /// <summary>
    /// Sets the header component.
    /// </summary>
    public CacheKeyBuilder WithHeader(string? header)
    {
        _header = header;
        return this;
    }

    /// <summary>
    /// Adds metadata.
    /// </summary>
    public CacheKeyBuilder WithMetadata(string key, string value)
    {
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the CacheKey instance.
    /// </summary>
    public CacheKey Build()
    {
        return new CacheKey(
            _route,
            _queryParams.Count > 0 ? _queryParams.ToImmutableDictionary() : null,
            _culture,
            _header,
            _metadata.Count > 0 ? _metadata.ToImmutableDictionary() : null);
    }

    /// <summary>
    /// Implicitly converts the builder to a CacheKey by calling Build().
    /// </summary>
    public static implicit operator CacheKey(CacheKeyBuilder builder) => builder.Build();

    /// <summary>
    /// Implicitly converts the builder to a string by building and converting to string.
    /// </summary>
    public static implicit operator string(CacheKeyBuilder builder) => builder.Build().ToString();
}
