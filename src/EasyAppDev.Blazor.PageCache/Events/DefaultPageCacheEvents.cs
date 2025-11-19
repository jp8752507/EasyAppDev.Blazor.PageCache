using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Events;

/// <summary>
/// Default no-op implementation of <see cref="IPageCacheEvents"/>.
/// </summary>
public sealed class DefaultPageCacheEvents : IPageCacheEvents
{
    /// <inheritdoc />
    public Task OnCacheHitAsync(CacheHitContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnCacheMissAsync(CacheMissContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnCacheSetAsync(CacheSetContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnCacheInvalidatedAsync(InvalidationContext context)
    {
        return Task.CompletedTask;
    }
}
