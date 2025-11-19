# EasyAppDev.Blazor.PageCache

[![NuGet](https://img.shields.io/nuget/v/EasyAppDev.Blazor.PageCache.svg)](https://www.nuget.org/packages/EasyAppDev.Blazor.PageCache/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-blue)](https://dotnet.microsoft.com/download)

Lightweight, high-performance HTML response caching for **Static Server-Side Rendered (SSR)** Blazor pages. Dramatically improve page load times with declarative `[PageCache]` attributes.

**✅ Best For:**
- Static SSR pages (no `@rendermode` directive)
- Static page wrappers with selective component-level interactivity

**⚠️ Important Limitation:**
- **NOT effective** for pages with `@rendermode InteractiveServer/WebAssembly/Auto` at the page level
- Component re-rendering after SignalR/WASM initialization overwrites cached values
- See [When to Use](#when-to-use) section for details

## Features

### Core Caching
- **🚀 Declarative Caching** - Mark pages with `[PageCache]` attribute
- **⚡ 20-50x Performance** - Serve cached pages in 2-5ms instead of 100-200ms
- **🛡️ Cache Stampede Prevention** - Built-in request coalescing
- **🔧 Flexible Cache Keys** - Vary by query parameters, headers, route values, culture
- **🏷️ Tag-Based Invalidation** - Group and invalidate related pages
- **📊 Diagnostics** - Real-time statistics and cache monitoring

### Security & Validation
- **🔒 Content Validation** - Pluggable validators for XSS detection and size limits
- **🛡️ DoS Prevention** - Rate limiting and memory exhaustion protection
- **🔐 Safe Defaults** - Authenticated user caching disabled by default

### Advanced Features
- **⚙️ Pluggable Storage** - Extensible cache storage backends (Memory, Redis-ready)
- **🔄 Custom Eviction Policies** - LRU, LFU, size-based, or build your own
- **🔑 Structured Cache Keys** - Type-safe cache keys with fluent builder API
- **🎨 Compression Strategies** - GZip, Brotli, or custom compression
- **🎯 Event Hooks** - Capture cache hits, misses, invalidations

### Framework Support
- **🎯 .NET 8 & 9** - Multi-targeted for latest frameworks
- **🔌 Extensible Architecture** - All major components implement interfaces

## Quick Start

### Installation

Install via NuGet Package Manager or CLI:

**NuGet Package Manager:**
```
Install-Package EasyAppDev.Blazor.PageCache
```

**.NET CLI:**
```bash
dotnet add package EasyAppDev.Blazor.PageCache
```

**Package Reference:**
```xml
<PackageReference Include="EasyAppDev.Blazor.PageCache" Version="1.0.0-preview.1" />
```

📦 [View on NuGet.org](https://www.nuget.org/packages/EasyAppDev.Blazor.PageCache/)

### Basic Configuration

```csharp
// Program.cs
using EasyAppDev.Blazor.PageCache.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add page caching
builder.Services.AddPageCache(options =>
{
    options.DefaultDurationSeconds = 300; // 5 minutes
    options.EnableStatistics = true;
});

var app = builder.Build();

app.UseStaticFiles();

// Enable page cache middleware (must be before UseAntiforgery)
app.UsePageCacheCapture();
app.UsePageCacheServe();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### Basic Usage

```razor
@page "/about"
@attribute [PageCache(Duration = 3600)] // Cache for 1 hour

<PageTitle>About Us</PageTitle>

<h1>About Us</h1>
<p>This page is cached!</p>
<p>Rendered at: @DateTime.Now</p>
```

## Advanced Configuration

### With Security Validation

Protect your cache from XSS attacks and memory exhaustion:

```csharp
using EasyAppDev.Blazor.PageCache.Extensions;
using EasyAppDev.Blazor.PageCache.Configuration;
using EasyAppDev.Blazor.PageCache.Validation;
using EasyAppDev.Blazor.PageCache.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Configure security options
builder.Services.Configure<SecurityOptions>(options =>
{
    options.EnableHtmlValidation = true;           // XSS protection
    options.MaxEntrySizeBytes = 5 * 1024 * 1024;   // 5 MB limit
    options.WarnOnLargeEntrySizeBytes = 1024 * 1024; // 1 MB warning
    options.EnableRateLimiting = true;             // DoS prevention
    options.CacheForAuthenticatedUsers = false;    // Safe default
});

// Register content validators
builder.Services.AddSingleton<IContentValidator>(sp =>
{
    var securityOptions = sp.GetRequiredService<IOptions<SecurityOptions>>();
    var logger = sp.GetRequiredService<ILogger<CompositeContentValidator>>();

    var validators = new List<IContentValidator>
    {
        new SizeLimitValidator(securityOptions,
            sp.GetRequiredService<ILogger<SizeLimitValidator>>()),
        new HtmlSanitizerValidator(securityOptions,
            sp.GetRequiredService<ILogger<HtmlSanitizerValidator>>())
    };

    return new CompositeContentValidator(validators, logger);
});

builder.Services.AddPageCache(options =>
{
    options.DefaultDurationSeconds = 300;
    options.MaxCacheSizeMB = 100;
    options.CompressCachedContent = true; // Enable compression
});
```

### With Compression

Choose your compression strategy:

```csharp
using EasyAppDev.Blazor.PageCache.Compression;

// Option 1: Using fluent builder
builder.Services.AddPageCache(builder => builder
    .UseCompression<BrotliCompressionStrategy>() // Better compression
    .Configure(options =>
    {
        options.DefaultDurationSeconds = 300;
    })
);

// Option 2: Using options
builder.Services.AddPageCache(options =>
{
    options.CompressCachedContent = true; // Uses GZip by default
});
```

### With Structured Cache Keys

Type-safe cache keys reduce errors:

```csharp
using EasyAppDev.Blazor.PageCache.Models;

// Build structured cache keys
var cacheKey = CacheKey.Create("/products")
    .WithQueryParam("category", "electronics")
    .WithQueryParam("sort", "price")
    .WithCulture("en-US")
    .Build();

// Use with cache service
await cacheService.SetCachedHtmlAsync(cacheKey, html, 300);
```

### With Custom Eviction Policies

Control how cache entries are evicted:

```csharp
using EasyAppDev.Blazor.PageCache.Eviction;

// LRU (Least Recently Used) - evict old entries first
var lruPolicy = new LruEvictionPolicy(maxAge: TimeSpan.FromHours(1));

// LFU (Least Frequently Used) - evict rarely accessed entries
var lfuPolicy = new LfuEvictionPolicy(minAccessThreshold: 10);

// Size-based - evict largest entries first
var sizePolicy = new SizeBasedEvictionPolicy(
    maxEntrySizeBytes: 2 * 1024 * 1024,
    strategy: SizeBasedEvictionPolicy.EvictionStrategy.LargestFirst);

// Composite - combine multiple strategies
var compositePolicy = new CompositeEvictionPolicy(
    CompositeEvictionPolicy.CompositionMode.WeightedPriority,
    new LruEvictionPolicy(),
    new SizeBasedEvictionPolicy());
```

### Complete Production Setup

```csharp
using EasyAppDev.Blazor.PageCache.Extensions;
using EasyAppDev.Blazor.PageCache.Configuration;
using EasyAppDev.Blazor.PageCache.Validation;
using EasyAppDev.Blazor.PageCache.Compression;

var builder = WebApplication.CreateBuilder(args);

// Security configuration
builder.Services.Configure<SecurityOptions>(options =>
{
    options.EnableHtmlValidation = true;
    options.MaxEntrySizeBytes = 5 * 1024 * 1024;
    options.EnableRateLimiting = true;
});

// Content validators
builder.Services.AddSingleton<IContentValidator>(sp =>
{
    var securityOptions = sp.GetRequiredService<IOptions<SecurityOptions>>();
    var logger = sp.GetRequiredService<ILogger<CompositeContentValidator>>();

    var validators = new List<IContentValidator>
    {
        new SizeLimitValidator(securityOptions,
            sp.GetRequiredService<ILogger<SizeLimitValidator>>()),
        new HtmlSanitizerValidator(securityOptions,
            sp.GetRequiredService<ILogger<HtmlSanitizerValidator>>())
    };

    return new CompositeContentValidator(validators, logger);
});

// Page cache with all features
builder.Services.AddPageCache(builder => builder
    .UseCompression<BrotliCompressionStrategy>()
    .Configure(options =>
    {
        options.DefaultDurationSeconds = 300;
        options.MaxCacheSizeMB = 100;
        options.VaryByCulture = true;
        options.EnableStatistics = true;
    })
);

var app = builder.Build();

app.UseStaticFiles();
app.UsePageCacheCapture();
app.UsePageCacheServe();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

## Usage Examples

### Simple Caching

```razor
@page "/features"
@attribute [PageCache(Duration = 1800)] // 30 minutes

<h1>Features</h1>
<p>This static content is cached for 30 minutes.</p>
```

### Vary By Query Parameters

```razor
@page "/blog"
@attribute [PageCache(
    Duration = 1800,
    VaryByQueryKeys = new[] { "page", "category" }
)]

<h1>Blog Posts</h1>
<!-- Different query parameters create separate cache entries -->
```

### Tag-Based Invalidation

```razor
@page "/products/{id:int}"
@attribute [PageCache(
    Duration = 3600,
    Tags = new[] { "products", "catalog" }
)]

<h1>Product Details</h1>
```

```csharp
// In your service
public class ProductService
{
    private readonly IPageCacheInvalidator _invalidator;

    public async Task UpdateProduct(int id)
    {
        await _db.SaveChangesAsync();

        // Invalidate all product pages
        _invalidator.InvalidateByTag("products");
    }
}
```

### Mixed Approach (Static + Interactive)

```razor
@page "/products"
@attribute [PageCache(Duration = 3600)]
@* Page wrapper is Static SSR (cached) *@

<h1>Our Products</h1>

<!-- Static content - fully cached -->
<div class="product-grid">
    @foreach (var product in Products)
    {
        <ProductCard Product="@product" />
    }
</div>

<!-- ONLY this component is interactive -->
<ProductFilter @rendermode="InteractiveServer" />

@code {
    private List<Product> Products = GetProducts();
}
```

### Cache Statistics

```razor
@page "/admin/cache-stats"
@inject IServiceProvider ServiceProvider
@using EasyAppDev.Blazor.PageCache.Extensions

@code {
    private PageCacheStats? stats;

    protected override void OnInitialized()
    {
        stats = ServiceProvider.GetCacheStats();
    }
}

<h1>Cache Statistics</h1>
<p>Hit Rate: @stats.HitRate.ToString("P2")</p>
<p>Total Requests: @stats.TotalRequests.ToString("N0")</p>
<p>Cache Size: @stats.CacheSizeMB.ToString("F2") MB</p>
```

## Configuration Reference

### PageCacheOptions

```csharp
builder.Services.AddPageCache(options =>
{
    // Basic settings
    options.Enabled = true;
    options.DefaultDurationSeconds = 300;

    // Cache key customization
    options.CacheKeyPrefix = "PageCache:";
    options.VaryByCulture = true;

    // Query parameter filtering
    options.IgnoredQueryParameters.Add("utm_source");
    options.IgnoredQueryParameters.Add("fbclid");

    // Cache limits
    options.MaxCacheSizeMB = 100;
    options.SlidingExpirationSeconds = 60;

    // Compression
    options.CompressCachedContent = false; // Or set CompressionStrategyType

    // Statistics
    options.EnableStatistics = true;

    // Cache stampede prevention
    options.CacheGenerationTimeoutSeconds = 30;
    options.MaxConcurrentCacheGenerations = 1;

    // Response filtering
    options.CacheOnlySuccessfulResponses = true;
    options.CacheableStatusCodes = new HashSet<int> { 200 };
});
```

### SecurityOptions

```csharp
builder.Services.Configure<SecurityOptions>(options =>
{
    // HTML Validation
    options.EnableHtmlValidation = false; // Disabled by default
    options.MaxScriptTagsAllowed = 50;

    // Size Validation
    options.EnableSizeValidation = true;
    options.MaxEntrySizeBytes = 5 * 1024 * 1024; // 5 MB
    options.WarnOnLargeEntrySizeBytes = 1024 * 1024; // 1 MB

    // Rate Limiting
    options.EnableRateLimiting = true;
    options.RateLimitMaxAttempts = 10;
    options.RateLimitWindowSeconds = 60;

    // Authenticated Users
    options.CacheForAuthenticatedUsers = false; // Disabled by default

    // Validation Behavior
    options.BlockOnValidationFailure = true;
    options.LogSecurityEvents = true;
});
```

## Cache Invalidation

```csharp
@inject IPageCacheInvalidator Invalidator

// Invalidate specific route
Invalidator.InvalidateRoute("/products/123");

// Invalidate pattern
Invalidator.InvalidatePattern("/blog/*");

// Invalidate by tag
Invalidator.InvalidateByTag("products");

// Clear all
Invalidator.ClearAll();
```

## Performance

Typical performance improvements:

| Scenario | Without Cache | With Cache | Improvement |
|----------|--------------|------------|-------------|
| Simple Page | 100-200ms | 2-5ms | **20-50x** |
| Complex Page | 300-500ms | 3-7ms | **40-100x** |
| With Database | 500-1000ms | 3-7ms | **100-300x** |

## When to Use

### ✅ Recommended Use Cases

**Full Page Caching (Static SSR):**
```razor
@page "/about"
@attribute [PageCache(Duration = 3600)]
@* No @rendermode = Static SSR = Full caching ✅ *@
```
- ✅ Static content pages (About, Features, Contact)
- ✅ Blog posts and articles
- ✅ Documentation pages
- ✅ Marketing/landing pages
- ✅ Product catalogs (read-only)

**Mixed Approach (Static Wrapper + Selective Interactivity):**
```razor
@page "/products"
@attribute [PageCache(Duration = 1800)]

<ProductGrid /> <!-- Cached -->
<ProductFilter @rendermode="InteractiveServer" /> <!-- Interactive -->
```
- ✅ Pages with mostly static content + small interactive sections
- ✅ Product/catalog pages with filters
- ✅ Blog with interactive comments section
- ✅ Documentation with search component

### ❌ NOT Recommended

**Pages with Full Page Interactive Mode:**
```razor
@page "/dashboard"
@rendermode InteractiveServer  @* ← Breaks caching! *@
@attribute [PageCache(Duration = 60)]
```
- ❌ Component re-renders after SignalR connects
- ❌ Cached values immediately overwritten
- ❌ **No performance benefit for users**

**Other Unsuitable Scenarios:**
- ❌ Pages with user-specific content
- ❌ Forms with anti-forgery tokens
- ❌ Real-time data displays
- ❌ Authenticated user dashboards
- ❌ Pages with `@rendermode` at page level

### ⚠️ Why Interactive Render Modes Don't Work

When a page has `@rendermode InteractiveServer/WebAssembly/Auto`:

1. ✅ HTTP middleware caches initial HTML
2. ✅ Browser receives cached HTML (fast!)
3. ❌ Blazor JavaScript initializes
4. ❌ SignalR/WASM connection established
5. ❌ **Component `OnInitialized()` runs AGAIN**
6. ❌ **New values generated, overwriting cache**

**Result:** Users see fresh values every time, defeating the cache purpose.

**Solution:** Use Static SSR with selective component-level interactivity.

## Architecture

### How It Works

```
Request → Middleware → Check Cache → [HIT] → Return Cached HTML (Fast!)
                            ↓
                         [MISS]
                            ↓
                    Render Page → Capture HTML → Store in Cache → Return HTML
```

### What Gets Cached

| Page Type | Initial HTML | User Experience | Effective? |
|-----------|-------------|----------------|------------|
| **Static SSR** (no `@rendermode`) | ✅ Cached | ✅ Fast loads, no re-render | ✅ **YES** |
| **Static wrapper** + component `@rendermode` | ✅ Cached | ✅ Fast initial load, component interactive | ✅ **YES** |
| **Page-level** `@rendermode InteractiveServer` | ⚠️ Cached | ❌ Component re-renders after SignalR | ❌ **NO** |
| **Page-level** `@rendermode InteractiveWebAssembly` | ⚠️ Cached | ❌ Component re-renders after WASM loads | ❌ **NO** |

**Key Insight:** Caching works at the HTTP level, but interactive components re-initialize client-side, overwriting cached values.

## Extensibility

### Custom Storage Backend

```csharp
using EasyAppDev.Blazor.PageCache.Abstractions;

public class RedisCacheStorage : ICacheStorage
{
    public ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        // Your Redis implementation
    }

    public ValueTask SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default)
    {
        // Your Redis implementation
    }

    // ... other methods
}

// Register
builder.Services.AddSingleton<ICacheStorage, RedisCacheStorage>();
```

### Custom Content Validator

```csharp
using EasyAppDev.Blazor.PageCache.Abstractions;

public class CustomValidator : IContentValidator
{
    public Task<ValidationResult> ValidateAsync(
        string content,
        string cacheKey,
        CancellationToken ct = default)
    {
        // Your validation logic
        if (content.Contains("forbidden-pattern"))
        {
            return Task.FromResult(ValidationResult.Failure(
                "Content contains forbidden pattern",
                ValidationSeverity.Critical));
        }

        return Task.FromResult(ValidationResult.Success());
    }
}

// Register
builder.Services.AddSingleton<IContentValidator, CustomValidator>();
```

### Custom Key Generator

```csharp
using EasyAppDev.Blazor.PageCache.Abstractions;

public class CustomKeyGenerator : ICacheKeyGenerator
{
    public string GenerateKey(HttpContext context, PageCacheAttribute? attribute = null)
    {
        // Your custom key generation logic
        return $"custom:{context.Request.Path}";
    }

    public bool IsCacheable(HttpContext context)
    {
        // Your cacheability rules
        return context.Response.StatusCode == 200;
    }
}

// Register
builder.Services.AddSingleton<ICacheKeyGenerator, CustomKeyGenerator>();
```

### Event Hooks

```csharp
using EasyAppDev.Blazor.PageCache.Abstractions;

public class MetricsEventHandler : IPageCacheEvents
{
    public Task OnCacheHitAsync(CacheHitContext context)
    {
        // Track cache hit metrics
        return Task.CompletedTask;
    }

    public Task OnCacheMissAsync(CacheMissContext context)
    {
        // Track cache miss metrics
        return Task.CompletedTask;
    }

    public Task OnCacheSetAsync(CacheSetContext context)
    {
        // Track cache set operations
        return Task.CompletedTask;
    }

    public Task OnCacheInvalidatedAsync(InvalidationContext context)
    {
        // Track invalidations
        return Task.CompletedTask;
    }
}

// Register
builder.Services.AddSingleton<IPageCacheEvents, MetricsEventHandler>();
```

## Security Considerations

### XSS Protection

Enable HTML validation to detect malicious content:

```csharp
builder.Services.Configure<SecurityOptions>(options =>
{
    options.EnableHtmlValidation = true;
});
```

**Detected Patterns:**
- Inline event handlers (`onclick`, `onerror`)
- JavaScript URLs (`javascript:`)
- Data URLs with scripts
- Base64-encoded malicious code
- Excessive script tags

### DoS Prevention

Protect against memory exhaustion:

```csharp
builder.Services.Configure<SecurityOptions>(options =>
{
    options.MaxEntrySizeBytes = 5 * 1024 * 1024; // 5 MB limit
    options.EnableRateLimiting = true;
});
```

### Authenticated Users

**Default:** Caching for authenticated users is **disabled** for security.

**If you need it:**
```csharp
builder.Services.Configure<SecurityOptions>(options =>
{
    options.CacheForAuthenticatedUsers = true; // Use with caution
});

// Or per-page
[PageCache(Duration = 60, CacheForAuthenticatedUsers = true)]
```

⚠️ **Warning:** Only enable if you understand the security implications and have proper cache key generation including user identity.

## Requirements

- .NET 8.0 or .NET 9.0
- ASP.NET Core Blazor (Server, WebAssembly Hosted, or Static SSR)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Built with ❤️ for the Blazor community.
