using System.IO;
using BlazorWasmRegex.Server;
using BlazorWasmRegex.Server.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Three modes:
// - Static (production): a "prerendered" folder exists under the content root. Pages are served from
//   those files and Razor components are never rendered, because the host is a tiny ARM board.
// - Prerender (image build): `--PrerenderTo <dir>` renders every page once into <dir> and exits.
// - Live (development, `dotnet run`): no prerendered folder, pages are rendered per request.
var builder = WebApplication.CreateBuilder(args);

var prerenderTo = builder.Configuration["PrerenderTo"];
var prerenderedDirectory = Path.Combine(builder.Environment.ContentRootPath, StaticPages.DirectoryName);
var serveStatic = prerenderTo is null && Directory.Exists(prerenderedDirectory);

builder.Services.AddRazorPages();
if (!serveStatic)
{
    builder.Services.AddRazorComponents()
        .AddInteractiveWebAssemblyComponents();
    // RegexTester is prerendered on the server, so it needs the same services as in the browser.
    BlazorWasmRegex.Client.Program.ConfigureServices(builder.Services);
}
if (prerenderTo is not null)
{
    // Any free local port; the prerenderer reads the actual address back.
    builder.WebHost.UseUrls("http://127.0.0.1:0");
}

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
if (!serveStatic)
{
    // Unknown paths render the not-found page with a 404 status.
    app.UseStatusCodePagesWithReExecute(StaticPages.NotFoundRoute, createScopeForStatusCodePages: true);
}

app.UseHttpsRedirection();

// Serves wwwroot and _framework files from the build-time static web assets manifest:
// precompressed (br/gzip) variants, ETags, and immutable caching for fingerprinted files.
app.MapStaticAssets();
app.MapRazorPages();

if (serveStatic)
{
    StaticPages.Map(app, prerenderedDirectory);
}
else
{
    app.MapRazorComponents<App>()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof(BlazorWasmRegex.Client._Imports).Assembly);
}

if (prerenderTo is not null)
{
    await app.StartAsync();
    var exitCode = await Prerenderer.RunAsync(app, prerenderTo);
    await app.StopAsync();
    return exitCode;
}

app.Run();
return 0;
