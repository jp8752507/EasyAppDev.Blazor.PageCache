using System.IO.Compression;
using System.Text;
using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Compression;

/// <summary>
/// GZip compression strategy for cached content.
/// </summary>
public sealed class GZipCompressionStrategy : ICompressionStrategy
{
    /// <inheritdoc />
    public string Name => "GZip";

    /// <inheritdoc />
    public byte[] Compress(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var bytes = Encoding.UTF8.GetBytes(content);
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Fastest))
        {
            gzipStream.Write(bytes, 0, bytes.Length);
        }
        return outputStream.ToArray();
    }

    /// <inheritdoc />
    public string Decompress(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var inputStream = new MemoryStream(data);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        gzipStream.CopyTo(outputStream);
        return Encoding.UTF8.GetString(outputStream.ToArray());
    }
}
