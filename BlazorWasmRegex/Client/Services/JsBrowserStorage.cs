#nullable enable
using System.Threading.Tasks;
using BlazorWasmRegex.Shared.Interfaces;
using Microsoft.JSInterop;

namespace BlazorWasmRegex.Client.Services
{
    /// <summary>
    /// Thin JS interop wrapper over <c>window.localStorage</c>. Exceptions (for example a
    /// <see cref="JSException"/> when storage is disabled) propagate to
    /// <see cref="BlazorWasmRegex.Shared.Services.LocalStorageService"/>, which handles them.
    /// </summary>
    // BL0016 (unguarded JS interop call) is suppressed on purpose: this wrapper stays thin, and every
    // failure (e.g. JSException when localStorage is disabled) is caught and logged by LocalStorageService,
    // which is where the fallback behaviour is unit-tested.
#pragma warning disable BL0016
    public sealed class JsBrowserStorage : IBrowserStorage
    {
        private readonly IJSRuntime _js;

        public JsBrowserStorage(IJSRuntime js)
        {
            _js = js;
        }

        public ValueTask<string?> GetItemAsync(string key) =>
            _js.InvokeAsync<string?>("localStorage.getItem", key);

        public ValueTask SetItemAsync(string key, string value) =>
            _js.InvokeVoidAsync("localStorage.setItem", key, value);
    }
#pragma warning restore BL0016
}
