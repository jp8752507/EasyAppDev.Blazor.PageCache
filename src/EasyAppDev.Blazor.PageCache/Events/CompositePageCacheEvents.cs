using EasyAppDev.Blazor.PageCache.Abstractions;

namespace EasyAppDev.Blazor.PageCache.Events;

/// <summary>
/// Composite event handler that dispatches events to multiple handlers.
/// </summary>
public sealed class CompositePageCacheEvents : IPageCacheEvents
{
    private readonly IEnumerable<IPageCacheEvents> _handlers;

    public CompositePageCacheEvents(IEnumerable<IPageCacheEvents> handlers)
    {
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
    }

    /// <inheritdoc />
    public async Task OnCacheHitAsync(CacheHitContext context)
    {
        foreach (var handler in _handlers)
        {
            await handler.OnCacheHitAsync(context);
        }
    }

    /// <inheritdoc />
    public async Task OnCacheMissAsync(CacheMissContext context)
    {
        foreach (var handler in _handlers)
        {
            await handler.OnCacheMissAsync(context);
        }
    }

    /// <inheritdoc />
    public async Task OnCacheSetAsync(CacheSetContext context)
    {
        foreach (var handler in _handlers)
        {
            await handler.OnCacheSetAsync(context);
        }
    }

    /// <inheritdoc />
    public async Task OnCacheInvalidatedAsync(InvalidationContext context)
    {
        foreach (var handler in _handlers)
        {
            await handler.OnCacheInvalidatedAsync(context);
        }
    }
}
