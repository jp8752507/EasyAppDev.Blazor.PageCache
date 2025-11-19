using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using EasyAppDev.Blazor.PageCache.Abstractions;
using EasyAppDev.Blazor.PageCache.Configuration;
using EasyAppDev.Blazor.PageCache.Services;
using EasyAppDev.Blazor.PageCache.Storage;
using EasyAppDev.Blazor.PageCache.Events;
using EasyAppDev.Blazor.PageCache.Compression;

namespace EasyAppDev.Blazor.PageCache.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add page caching services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Blazor page caching services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPageCache(this IServiceCollection services)
    {
        return services.AddPageCache(_ => { });
    }

    /// <summary>
    /// Adds Blazor page caching services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for <see cref="PageCacheOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPageCache(
        this IServiceCollection services,
        Action<PageCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<PageCacheOptions>, PageCacheOptionsValidator>());

        services.AddMemoryCache();

        services.TryAddSingleton<ICacheStorage, MemoryCacheStorage>();

        services.TryAddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();

        services.TryAddSingleton<IPageCacheEvents, DefaultPageCacheEvents>();

        services.TryAddSingleton<ICompressionStrategy>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<PageCacheOptions>>().Value;

            // Check if explicit type is set
            if (options.CompressionStrategyType != null)
            {
                return (ICompressionStrategy)ActivatorUtilities.CreateInstance(sp, options.CompressionStrategyType);
            }

            if (options.CompressCachedContent)
            {
                return new GZipCompressionStrategy();
            }

            return null!;
        });

        services.TryAddSingleton<AsyncKeyedLock>();

        services.TryAddSingleton<IPageCacheService, PageCacheService>();

        services.TryAddSingleton<IPageCacheInvalidator, PageCacheInvalidator>();

        services.TryAddScoped<Filters.PageCacheEndpointFilter>();

        return services;
    }

    /// <summary>
    /// Adds Blazor page caching services to the service collection using a fluent builder.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for the builder.</param>
    /// <returns>The builder for further configuration.</returns>
    public static PageCacheBuilder AddPageCacheBuilder(
        this IServiceCollection services,
        Action<PageCacheBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Create builder
        var builder = new PageCacheBuilder(services);

        // Configure using builder if provided
        configure?.Invoke(builder);

        // Build all services using existing AddPageCache
        services.AddPageCache(options => { });

        // Apply builder-specific registrations
        builder.Build();

        return builder;
    }
}
