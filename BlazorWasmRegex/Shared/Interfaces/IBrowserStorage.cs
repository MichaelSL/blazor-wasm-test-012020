#nullable enable
using System.Threading.Tasks;

namespace BlazorWasmRegex.Shared.Interfaces
{
    /// <summary>
    /// Raw string key/value access to the browser's <c>localStorage</c>.
    /// Implementations store and return values verbatim; any JSON encoding is done by
    /// <see cref="ILocalStorageService"/>.
    /// </summary>
    public interface IBrowserStorage
    {
        /// <summary>Returns the raw stored value, or <c>null</c> if the key does not exist.</summary>
        ValueTask<string?> GetItemAsync(string key);

        /// <summary>Stores <paramref name="value"/> verbatim under <paramref name="key"/>.</summary>
        ValueTask SetItemAsync(string key, string value);
    }
}
