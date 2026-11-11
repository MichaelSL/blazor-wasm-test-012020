using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BlazorStrap;
using BlazorWasmRegex.Shared.Interfaces;
using BlazorWasmRegex.Shared.Services;
using BlazorWasmRegex.Client.Services;

namespace BlazorWasmRegex.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // No root components: the server's pages mark where interactive components
            // (RegexTester) go, and blazor.web.js starts them.
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            ConfigureServices(builder.Services);

            await builder.Build().RunAsync();
        }

        // Also called by the server, which needs the same services to prerender RegexTester.
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddBlazorStrap();
            services.AddScoped<IBrowserStorage, JsBrowserStorage>();
            services.AddScoped<ILocalStorageService, LocalStorageService>();

            services.AddTransient<IRegexService, RegexService>();
            services.AddTransient<IHtmlHelperService, HtmlHelperService>();
            services.AddTransient<IRegexOptionsService, RegexOptionsService>();
        }
    }
}
