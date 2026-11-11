using BlazorWasmRegex.Shared.Interfaces;
using BlazorWasmRegex.Shared.Services;
using Xunit;

namespace RegexService.Tests
{
    public class LocalStorageServiceTests
    {
        private sealed class FakeBrowserStorage : IBrowserStorage
        {
            public Dictionary<string, string> Items { get; } = new();
            public bool Throw { get; set; }

            public ValueTask<string?> GetItemAsync(string key)
            {
                if (Throw) throw new InvalidOperationException("localStorage is not available");
                return ValueTask.FromResult(Items.TryGetValue(key, out var v) ? v : null);
            }

            public ValueTask SetItemAsync(string key, string value)
            {
                if (Throw) throw new InvalidOperationException("localStorage is not available");
                Items[key] = value;
                return ValueTask.CompletedTask;
            }
        }

        public static TheoryData<string> RoundTripValues => new()
        {
            "",
            "plain",
            @"^\d{3}-\d{4}$",
            "line1\r\nline2\nline3\ttab",
            "quote \" backslash \\ slash /",
            "<b>html</b> & 'apos' +",
            "unicode é ü 中文 😒 ⏱",
            "{\"IgnoreCase\":true,\"Multiline\":false,\"Singleline\":true}",
            "null",
            "123",
        };

        [Theory]
        [MemberData(nameof(RoundTripValues))]
        public async Task SetThenGet_RoundTripsValue(string value)
        {
            var storage = new FakeBrowserStorage();
            var service = new LocalStorageService(storage);

            await service.SetStringAsync("KEY", value);

            Assert.Equal(value, await service.GetStringAsync("KEY"));
        }

        [Fact]
        public async Task Set_WritesBlazoredFormat_QuotedJsonStringWithDefaultEscaping()
        {
            var storage = new FakeBrowserStorage();
            var service = new LocalStorageService(storage);

            await service.SetStringAsync("REGEX_SESSION_STORAGE_KEY", "a\"b\\c\nd<é>&😒");

            // Exact bytes System.Text.Json writes with a default JsonSerializerOptions, which is what
            // Blazored.LocalStorage 4.5.0 SetItemAsync<string> stored.
            Assert.Equal("\"a\\u0022b\\\\c\\nd\\u003C\\u00E9\\u003E\\u0026\\uD83D\\uDE12\"",
                storage.Items["REGEX_SESSION_STORAGE_KEY"]);
        }

        [Fact]
        public async Task Set_RegexOptionsJson_IsStoredAsJsonEncodedString()
        {
            var storage = new FakeBrowserStorage();
            var service = new LocalStorageService(storage);

            await service.SetStringAsync("REGEX_OPTIONS", "{\"IgnoreCase\":true,\"Multiline\":false,\"Singleline\":false}");

            Assert.Equal("\"{\\u0022IgnoreCase\\u0022:true,\\u0022Multiline\\u0022:false,\\u0022Singleline\\u0022:false}\"",
                storage.Items["REGEX_OPTIONS"]);
        }

        [Theory]
        // Values as Blazored.LocalStorage 4.5.0 stored them -> value the app should see.
        [InlineData("\"\\\\d\\u002B\"", @"\d+")]
        [InlineData("\"first\\r\\nsecond\\nthird\"", "first\r\nsecond\nthird")]
        [InlineData("\"caf\\u00E9 \\u4E2D\\u6587 \\uD83D\\uDE12\"", "café 中文 😒")]
        [InlineData("\"caf\u00e9 unescaped\"", "café unescaped")]
        [InlineData("\"\\u003Cb\\u003E \\u0026 \\u0027\"", "<b> & '")]
        [InlineData("\"{\\u0022IgnoreCase\\u0022:true,\\u0022Multiline\\u0022:false,\\u0022Singleline\\u0022:true}\"",
            "{\"IgnoreCase\":true,\"Multiline\":false,\"Singleline\":true}")]
        [InlineData("\"\"", "")]
        public async Task Get_ReadsBlazoredFormatValues(string stored, string expected)
        {
            var storage = new FakeBrowserStorage();
            storage.Items["KEY"] = stored;
            var service = new LocalStorageService(storage);

            Assert.Equal(expected, await service.GetStringAsync("KEY"));
        }

        [Theory]
        // Not a JSON string: Blazored fell back to the raw value.
        [InlineData(@"^\d+$")]
        [InlineData("abc|True|False|True")]
        [InlineData("\"unterminated")]
        [InlineData("123")]
        [InlineData("true")]
        [InlineData("{\"IgnoreCase\":true}")]
        [InlineData("[1,2]")]
        public async Task Get_NonJsonStringValue_FallsBackToRawValue(string stored)
        {
            var storage = new FakeBrowserStorage();
            storage.Items["KEY"] = stored;
            var service = new LocalStorageService(storage);

            Assert.Equal(stored, await service.GetStringAsync("KEY"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("null")]
        public async Task Get_MissingEmptyOrNullValue_ReturnsNull(string? stored)
        {
            var storage = new FakeBrowserStorage();
            if (stored != null) storage.Items["KEY"] = stored;
            var service = new LocalStorageService(storage);

            Assert.Null(await service.GetStringAsync("KEY"));
        }

        [Fact]
        public async Task StorageUnavailable_GetReturnsNullAndSetDoesNotThrow()
        {
            var storage = new FakeBrowserStorage { Throw = true };
            var service = new LocalStorageService(storage);

            Assert.Null(await service.GetStringAsync("KEY"));
            await service.SetStringAsync("KEY", "value");
            Assert.Empty(storage.Items);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task EmptyKey_Throws(string key)
        {
            var service = new LocalStorageService(new FakeBrowserStorage());

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetStringAsync(key));
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.SetStringAsync(key, "v"));
        }
    }
}
