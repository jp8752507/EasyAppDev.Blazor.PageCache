using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Configuration;
using EasyAppDev.Blazor.PageCache.Compression;
using EasyAppDev.Blazor.PageCache.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyAppDev.Blazor.PageCache.Extensions;

/// <summary>
/// Fluent builder for configuring page cache services.
/// </summary>
public sealed class PageCacheBuilder
{
    private readonly IServiceCollection _services;
    private Type? _storageType;
    private Type? _keyGeneratorType;
    private Type? _compressionStrategyType;
    private readonly List<Type> _eventHandlerTypes = new();
    private Action<PageCacheOptions>? _configureOptions;

    internal PageCacheBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Configures the cache storage implementation.
    /// </summary>
    /// <typeparam name="T">The storage implementation type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public PageCacheBuilder UseStorage<T>() where T : class, ICacheStorage
    {
        _storageType = typeof(T);
        return this;
    }

    /// <summary>
    /// Configures the cache key generator.
    /// </summary>
    /// <typeparam name="T">The key generator implementation type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public PageCacheBuilder UseKeyGenerator<T>() where T : class, ICacheKeyGenerator
    {
        _keyGeneratorType = typeof(T);
        return this;
    }

    /// <summary>
    /// Configures the compression strategy.
    /// </summary>
    /// <typeparam name="T">The compression strategy implementation type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public PageCacheBuilder UseCompression<T>() where T : class, ICompressionStrategy
    {
        _compressionStrategyType = typeof(T);
        return this;
    }

    /// <summary>
    /// Disables compression for cached content.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public PageCacheBuilder WithoutCompression()
    {
        _compressionStrategyType = null;
        return this;
    }

    /// <summary>
    /// Adds an event handler for cache lifecycle events.
    /// </summary>
    /// <typeparam name="T">The event handler implementation type.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public PageCacheBuilder AddEventHandler<T>() where T : class, IPageCacheEvents
    {
        _eventHandlerTypes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Configures page cache options.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    public PageCacheBuilder Configure(Action<PageCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configureOptions = configure;
        return this;
    }

    /// <summary>
    /// Builds and registers all page cache services.
    /// </summary>
    internal void Build()
    {
        if (_configureOptions != null)
        {
            _services.Configure(_configureOptions);
        }

        if (_compressionStrategyType != null)
        {
            _services.TryAddSingleton(typeof(ICompressionStrategy), _compressionStrategyType);
        }

        if (_storageType != null)
        {
            _services.TryAddSingleton(typeof(ICacheStorage), _storageType);
        }

        if (_keyGeneratorType != null)
        {
            _services.TryAddSingleton(typeof(ICacheKeyGenerator), _keyGeneratorType);
        }

        if (_eventHandlerTypes.Count > 0)
        {
            foreach (var handlerType in _eventHandlerTypes)
            {
                _services.TryAddSingleton(handlerType);
            }

            _services.TryAddSingleton<IPageCacheEvents>(sp =>
            {
                var handlers = _eventHandlerTypes
                    .Select(t => (IPageCacheEvents)sp.GetRequiredService(t))
                    .ToList();

                return handlers.Count == 1
                    ? handlers[0]
                    : new CompositePageCacheEvents(handlers);
            });
        }
    }
}
