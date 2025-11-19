using System.Text;

namespace EasyAppDev.Blazor.PageCache.Diagnostics;

/// <summary>
/// Statistics and diagnostic information about the page cache.
/// </summary>
public sealed class PageCacheStats
{
    /// <summary>
    /// Gets the number of cache hits.
    /// </summary>
    public long HitCount { get; init; }

    /// <summary>
    /// Gets the number of cache misses.
    /// </summary>
    public long MissCount { get; init; }

    /// <summary>
    /// Gets the total number of cache requests.
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Gets the cache hit rate (0.0 to 1.0).
    /// </summary>
    public double HitRate { get; init; }

    /// <summary>
    /// Gets the number of currently cached entries.
    /// </summary>
    public int CachedEntries { get; init; }

    /// <summary>
    /// Gets the total size of cached content in bytes.
    /// </summary>
    public long CacheSizeBytes { get; init; }

    /// <summary>
    /// Gets the cache size in megabytes.
    /// </summary>
    public double CacheSizeMB => CacheSizeBytes / 1024.0 / 1024.0;

    /// <summary>
    /// Gets the average cached page size in bytes.
    /// </summary>
    public double AveragePageSizeBytes => CachedEntries > 0
        ? (double)CacheSizeBytes / CachedEntries
        : 0;

    /// <summary>
    /// Gets the number of cache evictions.
    /// </summary>
    public long EvictionCount { get; init; }

    /// <summary>
    /// Gets the timestamp when statistics collection started.
    /// </summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// Gets the duration for which statistics have been collected.
    /// </summary>
    public TimeSpan Duration => DateTimeOffset.UtcNow - StartTime;

    /// <summary>
    /// Gets requests per second.
    /// </summary>
    public double RequestsPerSecond => Duration.TotalSeconds > 0
        ? TotalRequests / Duration.TotalSeconds
        : 0;

    /// <summary>
    /// Returns a formatted string representation of the statistics.
    /// </summary>
    public override string ToString()
    {
        return $"Cache Stats: {HitCount} hits, {MissCount} misses, " +
               $"{HitRate:P2} hit rate, {CachedEntries} entries, " +
               $"{CacheSizeMB:F2} MB";
    }

    /// <summary>
    /// Gets a detailed diagnostic report.
    /// </summary>
    /// <returns>A formatted report of cache statistics.</returns>
    public string GetDetailedReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Page Cache Statistics ===");
        sb.AppendLine($"Hit Rate:          {HitRate:P2}");
        sb.AppendLine($"Total Requests:    {TotalRequests:N0}");
        sb.AppendLine($"Cache Hits:        {HitCount:N0}");
        sb.AppendLine($"Cache Misses:      {MissCount:N0}");
        sb.AppendLine($"Cached Entries:    {CachedEntries:N0}");
        sb.AppendLine($"Cache Size:        {CacheSizeMB:F2} MB");
        sb.AppendLine($"Avg Page Size:     {AveragePageSizeBytes:F0} bytes");
        sb.AppendLine($"Evictions:         {EvictionCount:N0}");
        sb.AppendLine($"Uptime:            {Duration}");
        sb.AppendLine($"Requests/Second:   {RequestsPerSecond:F2}");
        return sb.ToString();
    }
}
