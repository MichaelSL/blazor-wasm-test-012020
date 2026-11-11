using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// Serves the client's wwwroot and _framework files from the build-time static web
// assets manifest: precompressed (br/gzip) variants, ETags, and immutable caching
// for fingerprinted files. Replaces UseBlazorFrameworkFiles() + UseStaticFiles().
app.MapStaticAssets();
app.MapRazorPages();

// index.html references fingerprinted framework files, so it must be revalidated on every
// load rather than heuristically cached.
static void NoCache(StaticFileResponseContext ctx) =>
    ctx.Context.Response.Headers.CacheControl = "no-cache";

// Known client-side routes (keep in sync with the @page directives in BlazorWasmRegex/Client/Pages)
// get index.html with 200.
foreach (var route in new[] { "/", "/about" })
{
    app.MapFallbackToFile(route, "index.html", new StaticFileOptions { OnPrepareResponse = NoCache });
}

// Any other path still gets index.html, so the Blazor router shows its "not found" page, but with
// a 404 status, so search engines don't index unknown URLs as duplicates of the home page.
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        NoCache(ctx);
        ctx.Context.Response.StatusCode = StatusCodes.Status404NotFound;
    }
});

app.Run();
