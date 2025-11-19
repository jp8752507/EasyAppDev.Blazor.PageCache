using System.IO.Compression;
using System.Text;
using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Compression;

/// <summary>
/// Brotli compression strategy for cached content.
/// Provides better compression ratios than GZip at the cost of slightly more CPU usage.
/// </summary>
public sealed class BrotliCompressionStrategy : ICompressionStrategy
{
    /// <inheritdoc />
    public string Name => "Brotli";

    /// <inheritdoc />
    public byte[] Compress(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var bytes = Encoding.UTF8.GetBytes(content);
        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(outputStream, CompressionLevel.Fastest))
        {
            brotliStream.Write(bytes, 0, bytes.Length);
        }
        return outputStream.ToArray();
    }

    /// <inheritdoc />
    public string Decompress(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var inputStream = new MemoryStream(data);
        using var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        brotliStream.CopyTo(outputStream);
        return Encoding.UTF8.GetString(outputStream.ToArray());
    }
}
