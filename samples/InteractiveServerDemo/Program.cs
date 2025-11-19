using InteractiveServerDemo.Components;
using EasyAppDev.Blazor.PageCache.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add page caching
builder.Services.AddPageCache(options =>
{
    options.Enabled = true;
    options.DefaultDurationSeconds = 60; // 1 minute for testing
    options.EnableStatistics = true;
    options.MaxCacheSizeMB = 100;
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
