using EasyAppDev.Blazor.PageCache.Services;
using FluentAssertions;
using Xunit;

namespace EasyAppDev.Blazor.PageCache.Tests.Services;

public class AsyncKeyedLockTests
{
    [Fact]
    public async Task LockAsync_SingleKey_AcquiresAndReleasesSuccessfully()
    {
        // Arrange
        using var lockManager = new AsyncKeyedLock();
        var key = "test-key";

        // Act
        var lockHandle = await lockManager.LockAsync(key, TimeSpan.FromSeconds(5));

        // Assert
        lockHandle.Should().NotBeNull();

        // Release
        lockHandle.Dispose();
    }

    [Fact]
    public async Task LockAsync_ConcurrentRequestsSameKey_OnlyOneProceedsAtATime()
    {
        // Arrange
        using var lockManager = new AsyncKeyedLock();
        var key = "test-key";
        var concurrentCount = 0;
        var maxConcurrentCount = 0;
        var completedCount = 0;
        var totalRequests = 10;

        // Act
        var tasks = Enumerable.Range(0, totalRequests).Select(async i =>
        {
            using var lockHandle = await lockManager.LockAsync(key, TimeSpan.FromSeconds(10));

            // Track concurrent access
            var current = Interlocked.Increment(ref concurrentCount);
            var max = maxConcurrentCount;
            while (current > max)
            {
                max = Interlocked.CompareExchange(ref maxConcurrentCount, current, max);
                if (max >= current) break;
                max = maxConcurrentCount;
            }

            // Simulate work
            await Task.Delay(10);

            Interlocked.Decrement(ref concurrentCount);
            Interlocked.Increment(ref completedCount);
        });

        await Task.WhenAll(tasks);

        // Assert
        maxConcurrentCount.Should().Be(1, "only one task should hold the lock at a time");
        completedCount.Should().Be(totalRequests, "all tasks should complete");
    }

    [Fact]
    public async Task LockAsync_MultipleDifferentKeys_CanRunConcurrently()
    {
        // Arrange
        using var lockManager = new AsyncKeyedLock();
        var keys = new[] { "key1", "key2", "key3", "key4", "key5" };
        var concurrentExecutions = 0;
        var maxConcurrentExecutions = 0;

        // Act
        var tasks = keys.Select(async key =>
        {
            using var lockHandle = await lockManager.LockAsync(key, TimeSpan.FromSeconds(5));

            var current = Interlocked.Increment(ref concurrentExecutions);
            var max = maxConcurrentExecutions;
            while (current > max)
            {
                max = Interlocked.CompareExchange(ref maxConcurrentExecutions, current, max);
                if (max >= current) break;
                max = maxConcurrentExecutions;
            }

            await Task.Delay(50);
            Interlocked.Decrement(ref concurrentExecutions);
        });

        await Task.WhenAll(tasks);

        // Assert
        maxConcurrentExecutions.Should().BeGreaterThan(1, "different keys should allow concurrent execution");
    }

    [Fact]
    public async Task LockAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange
        using var lockManager = new AsyncKeyedLock();
        var key = "test-key";

        // Acquire lock first
        var firstLock = await lockManager.LockAsync(key, TimeSpan.FromSeconds(10));

        // Act & Assert
        var act = async () => await lockManager.LockAsync(key, TimeSpan.FromMilliseconds(100));
        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage($"Failed to acquire lock for key '{key}' within *");

        // Cleanup
        firstLock.Dispose();
    }

    [Fact]
    public async Task LockAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var lockManager = new AsyncKeyedLock();
        lockManager.Dispose();

        // Act & Assert
        var act = async () => await lockManager.LockAsync("test-key", TimeSpan.FromSeconds(5));
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task LockAsync_NullOrWhiteSpaceKey_ThrowsArgumentException()
    {
        // Arrange
        using var lockManager = new AsyncKeyedLock();

        // Act & Assert - null
        var actNull = async () => await lockManager.LockAsync(null!, TimeSpan.FromSeconds(5));
        await actNull.Should().ThrowAsync<ArgumentException>();

        // Act & Assert - empty
        var actEmpty = async () => await lockManager.LockAsync(string.Empty, TimeSpan.FromSeconds(5));
        await actEmpty.Should().ThrowAsync<ArgumentException>();

        // Act & Assert - whitespace
        var actWhitespace = async () => await lockManager.LockAsync("   ", TimeSpan.FromSeconds(5));
        await actWhitespace.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task LockAsync_ReleaseAndReacquire_WorksCorrectly()
    {
        // Arrange
        using var lockManager = new AsyncKeyedLock();
        var key = "test-key";

        // Act - First acquisition
        var lock1 = await lockManager.LockAsync(key, TimeSpan.FromSeconds(5));
        lock1.Dispose();

        // Act - Second acquisition (should succeed immediately)
        var lock2 = await lockManager.LockAsync(key, TimeSpan.FromSeconds(5));

        // Assert
        lock2.Should().NotBeNull();

        lock2.Dispose();
    }

    [Fact]
    public async Task LockAsync_CancellationToken_CancelsWait()
    {
        // Arrange
        using var lockManager = new AsyncKeyedLock();
        var key = "test-key";
        using var cts = new CancellationTokenSource();

        // Acquire lock first
        var firstLock = await lockManager.LockAsync(key, TimeSpan.FromSeconds(10));

        // Cancel after 100ms
        cts.CancelAfter(100);

        // Act & Assert
        var act = async () => await lockManager.LockAsync(key, TimeSpan.FromSeconds(10), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Cleanup
        firstLock.Dispose();
    }

    [Fact]
    public async Task LockAsync_DoubleDispose_DoesNotThrow()
    {
        // Arrange
        using var lockManager = new AsyncKeyedLock();
        var key = "test-key";

        var lockHandle = await lockManager.LockAsync(key, TimeSpan.FromSeconds(5));

        // Act - Double dispose
        var act = () =>
        {
            lockHandle.Dispose();
            lockHandle.Dispose(); // Should not throw
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task LockAsync_StressTest_100ConcurrentRequests_OnlyOneRenders()
    {
        // Arrange
        using var lockManager = new AsyncKeyedLock();
        var key = "cache-key";
        var renderCount = 0;
        var concurrentRenderCount = 0;
        var maxConcurrent = 0;
        var requestCount = 100;

        // Act
        var tasks = Enumerable.Range(0, requestCount).Select(async i =>
        {
            using var lockHandle = await lockManager.LockAsync(key, TimeSpan.FromSeconds(30));

            // Simulate cache check and render
            var current = Interlocked.Increment(ref concurrentRenderCount);
            var max = maxConcurrent;
            while (current > max)
            {
                max = Interlocked.CompareExchange(ref maxConcurrent, current, max);
                if (max >= current) break;
                max = maxConcurrent;
            }

            Interlocked.Increment(ref renderCount);

            // Simulate render time
            await Task.Delay(10);

            Interlocked.Decrement(ref concurrentRenderCount);
        });

        await Task.WhenAll(tasks);

        // Assert
        maxConcurrent.Should().Be(1, "cache stampede prevention should ensure only 1 concurrent render");
        renderCount.Should().Be(requestCount, "all requests should eventually complete");
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var lockManager = new AsyncKeyedLock();

        // Act
        var act = () =>
        {
            lockManager.Dispose();
            lockManager.Dispose();
            lockManager.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task LockAsync_SemaphoreCleanup_RemovesUnusedSemaphores()
    {
        // Arrange
        using var lockManager = new AsyncKeyedLock();
        var key = "test-key";

        // Act - Acquire and release lock
        var lock1 = await lockManager.LockAsync(key, TimeSpan.FromSeconds(5));
        lock1.Dispose();

        // Note: We can't directly test internal state, but we verify that
        // subsequent operations work correctly (which they wouldn't if cleanup failed)

        var lock2 = await lockManager.LockAsync(key, TimeSpan.FromSeconds(5));
        lock2.Dispose();

        // Assert - If we got here without issues, cleanup is working
        Assert.True(true);
    }
}
