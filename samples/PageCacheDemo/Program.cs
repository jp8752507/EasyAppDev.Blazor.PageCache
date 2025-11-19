using PageCacheDemo.Components;
using EasyAppDev.Blazor.PageCache.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add page caching with configuration
builder.Services.AddPageCache(options =>
{
    options.Enabled = true;
    options.DefaultDurationSeconds = 300; // 5 minutes default
    options.EnableStatistics = true;
    options.MaxCacheSizeMB = 100;
    options.VaryByCulture = true;
    options.CacheGenerationTimeoutSeconds = 30;

    // Ignore tracking parameters
    options.IgnoredQueryParameters.Add("utm_source");
    options.IgnoredQueryParameters.Add("utm_medium");
    options.IgnoredQueryParameters.Add("fbclid");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enable page cache middleware (must be before UseAntiforgery)
app.UsePageCache();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
