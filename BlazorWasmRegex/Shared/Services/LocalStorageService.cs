#nullable enable
using System;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorWasmRegex.Shared.Interfaces;

namespace BlazorWasmRegex.Shared.Services
{
    /// <summary>
    /// Minimal replacement for Blazored.LocalStorage 4.5.0 (unlisted from NuGet).
    /// Format compatibility, confirmed by decompiling Blazored.LocalStorage 4.5.0:
    /// <list type="bullet">
    /// <item><c>SetItemAsync&lt;T&gt;</c> stores <c>JsonSerializer.Serialize(data, new JsonSerializerOptions())</c>
    /// (default options plus a TimeSpan converter that does not affect strings), so a string is stored
    /// as a quoted JSON string with the default encoder's escapes (e.g. <c>\n</c>, <c>&</c>, <c>é</c>).</item>
    /// <item><c>GetItemAsync&lt;string&gt;</c> returns <c>default</c> for a missing, empty or whitespace-only
    /// value; otherwise deserializes it, and returns the raw text if it is not a JSON string
    /// (<c>JsonException</c> at path <c>$</c>).</item>
    /// </list>
    /// </summary>
    public class LocalStorageService : ILocalStorageService
    {
        // Same settings Blazored used: a default JsonSerializerOptions instance.
        private static readonly JsonSerializerOptions SerializerOptions = new();

        private readonly IBrowserStorage _storage;
        private bool _failureLogged;

        public LocalStorageService(IBrowserStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public async Task<string?> GetStringAsync(string key)
        {
            ValidateKey(key);
            string? raw;
            try
            {
                raw = await _storage.GetItemAsync(key);
            }
            catch (Exception ex)
            {
                LogFailure("read", key, ex);
                return null;
            }

            return Deserialize(raw);
        }

        public async Task SetStringAsync(string key, string? value)
        {
            ValidateKey(key);
            var serialized = Serialize(value);
            try
            {
                await _storage.SetItemAsync(key, serialized);
            }
            catch (Exception ex)
            {
                LogFailure("write", key, ex);
            }
        }

        /// <summary>Encodes a value exactly as Blazored.LocalStorage <c>SetItemAsync&lt;string&gt;</c> did.</summary>
        public static string Serialize(string? value) => JsonSerializer.Serialize(value, SerializerOptions);

        /// <summary>Decodes a stored value exactly as Blazored.LocalStorage <c>GetItemAsync&lt;string&gt;</c> did.</summary>
        /// <remarks>
        /// Blazored rethrew a <c>JsonException</c> whose path was not <c>$</c>; for a <c>string</c> target that
        /// does not occur in practice, but if it did this returns the raw value rather than failing app start-up.
        /// </remarks>
        public static string? Deserialize(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<string>(raw, SerializerOptions);
            }
            catch (JsonException)
            {
                // Not a JSON string (e.g. written by something other than Blazored): use the raw text.
                return raw;
            }
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
        }

        private void LogFailure(string operation, string key, Exception ex)
        {
            // localStorage can be unavailable (privacy settings, sandboxed iframes, quota). The app keeps
            // working without persistence; log the first failure only to avoid flooding the console.
            if (_failureLogged)
            {
                return;
            }

            _failureLogged = true;
            // JSException messages carry the JS stack on the following lines; the first line is enough.
            var message = ex.Message.Split('\n')[0].TrimEnd();
            Console.WriteLine($"Local storage {operation} failed for '{key}'; continuing without persistence. {ex.GetType().Name}: {message}");
        }
    }
}
