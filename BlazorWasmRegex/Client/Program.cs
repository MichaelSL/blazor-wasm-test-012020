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
using Blazored.LocalStorage;

namespace BlazorWasmRegex.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            ConfigureServices(builder.Services);

            await builder.Build().RunAsync();
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddBlazorStrap();
            services.AddBlazoredLocalStorage();

            services.AddTransient<IRegexService, RegexService>();
            services.AddTransient<IHtmlHelperService, HtmlHelperService>();
        }
    }
}
