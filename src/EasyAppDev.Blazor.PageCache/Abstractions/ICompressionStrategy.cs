namespace EasyAppDev.Blazor.PageCache.Abstractions;

/// <summary>
/// Defines a strategy for compressing and decompressing cached content.
/// </summary>
public interface ICompressionStrategy
{
    /// <summary>
    /// Compresses the specified content.
    /// </summary>
    /// <param name="content">The content to compress.</param>
    /// <returns>The compressed byte array.</returns>
    byte[] Compress(string content);

    /// <summary>
    /// Decompresses the specified data.
    /// </summary>
    /// <param name="data">The compressed data.</param>
    /// <returns>The decompressed content string.</returns>
    string Decompress(byte[] data);

    /// <summary>
    /// Gets the name of the compression strategy.
    /// </summary>
    string Name { get; }
}
