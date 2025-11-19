using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using EasyAppDev.Blazor.PageCache.Filters;

namespace EasyAppDev.Blazor.PageCache.Extensions;

/// <summary>
/// Extension methods for <see cref="RazorComponentsEndpointConventionBuilder"/>.
/// </summary>
public static class RazorComponentsEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds page caching filter to Razor component endpoints.
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static RazorComponentsEndpointConventionBuilder AddPageCacheFilter(
        this RazorComponentsEndpointConventionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Finally(endpointBuilder =>
        {
            endpointBuilder.FilterFactories.Add((context, next) =>
            {
                return async invocationContext =>
                {
                    var filter = invocationContext.HttpContext.RequestServices
                        .GetRequiredService<PageCacheEndpointFilter>();
                    return await filter.InvokeAsync(invocationContext, next);
                };
            });
        });

        return builder;
    }
}
