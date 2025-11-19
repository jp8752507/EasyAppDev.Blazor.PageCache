using System.Collections.Concurrent;

namespace EasyAppDev.Blazor.PageCache.Services;

/// <summary>
/// Provides keyed asynchronous locks to prevent cache stampede.
/// </summary>
public sealed class AsyncKeyedLock : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, int> _lockCounts = new();
    private bool _disposed;

    /// <summary>
    /// Acquires a lock for the specified key.
    /// </summary>
    /// <param name="key">The key to lock on.</param>
    /// <param name="timeout">Maximum time to wait for lock acquisition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable lock that must be released.</returns>
    public async Task<IDisposable> LockAsync(
        string key,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // Get or create semaphore for this key
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        // Increment reference count
        _lockCounts.AddOrUpdate(key, 1, (_, count) => count + 1);

        // Wait for lock
        var acquired = await semaphore.WaitAsync(timeout, cancellationToken);

        if (!acquired)
        {
            // Timeout - decrement count and throw
            DecrementLockCount(key);
            throw new TimeoutException($"Failed to acquire lock for key '{key}' within {timeout}");
        }

        return new LockReleaser(this, key, semaphore);
    }

    /// <summary>
    /// Releases a lock and performs cleanup if needed.
    /// </summary>
    private void ReleaseLock(string key, SemaphoreSlim semaphore)
    {
        semaphore.Release();
        DecrementLockCount(key);
    }

    /// <summary>
    /// Decrements the reference count for a lock and cleans up if needed.
    /// Uses atomic operations to prevent race conditions.
    /// </summary>
    private void DecrementLockCount(string key)
    {
        // Atomically decrement the count
        var newCount = _lockCounts.AddOrUpdate(
            key,
            0, // Should not happen, but safe default
            (_, currentCount) => currentCount - 1);

        // If count is now zero or below, try to clean up
        if (newCount <= 0)
        {
            // Attempt to remove the count entry
            // This might fail if another thread incremented between AddOrUpdate and here
            if (_lockCounts.TryRemove(key, out var finalCount) && finalCount <= 0)
            {
                // Successfully removed and count is still <= 0, safe to dispose semaphore
                if (_locks.TryRemove(key, out var semaphore))
                {
                    semaphore.Dispose();
                }
            }
            // If TryRemove failed or finalCount > 0, another thread is using this key
            // In that case, do nothing - the semaphore is still needed
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var semaphore in _locks.Values)
        {
            semaphore.Dispose();
        }

        _locks.Clear();
        _lockCounts.Clear();
    }

    /// <summary>
    /// Lock releaser that implements IDisposable.
    /// </summary>
    private sealed class LockReleaser : IDisposable
    {
        private readonly AsyncKeyedLock _parent;
        private readonly string _key;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed;

        public LockReleaser(AsyncKeyedLock parent, string key, SemaphoreSlim semaphore)
        {
            _parent = parent;
            _key = key;
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _parent.ReleaseLock(_key, _semaphore);
        }
    }
}
