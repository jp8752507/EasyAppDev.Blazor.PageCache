using System.Text;
using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Compression;

/// <summary>
/// No-compression strategy that stores content as-is (null object pattern).
/// Useful for content that is already compressed or when compression overhead is not desired.
/// </summary>
public sealed class NoCompressionStrategy : ICompressionStrategy
{
    /// <inheritdoc />
    public string Name => "None";

    /// <inheritdoc />
    public byte[] Compress(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return Encoding.UTF8.GetBytes(content);
    }

    /// <inheritdoc />
    public string Decompress(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Encoding.UTF8.GetString(data);
    }
}
