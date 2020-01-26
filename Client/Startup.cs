using BlazorStrap;
using BlazorWasmRegexTest.Shared.Interfaces;
using BlazorWasmRegexTest.Shared.Services;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorWasmRegexTest.Client
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBootstrapCSS();

            services.AddTransient<IRegexService, RegexService>();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
