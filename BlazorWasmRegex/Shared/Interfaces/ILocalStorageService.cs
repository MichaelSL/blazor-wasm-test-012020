#nullable enable
using System.Threading.Tasks;

namespace BlazorWasmRegex.Shared.Interfaces
{
    /// <summary>
    /// Persists string values in browser local storage using the same on-disk format as
    /// Blazored.LocalStorage 4.x (<c>SetItemAsync&lt;string&gt;</c>/<c>GetItemAsync&lt;string&gt;</c>),
    /// so values saved by earlier versions of the app keep loading, and a rollback can read
    /// values saved by this version.
    /// Never throws because storage is unavailable: reads return <c>null</c> and writes are skipped.
    /// </summary>
    public interface ILocalStorageService
    {
        Task<string?> GetStringAsync(string key);

        Task SetStringAsync(string key, string? value);
    }
}
