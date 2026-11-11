using BlazorWasmRegex.Shared.Services;
using Xunit;

namespace RegexService.Tests
{
    public class AppVersionFormatterTests
    {
        [Theory]
        [InlineData("2026.11.10.57+a1b2c3d", "2026.11.10.57+a1b2c3d")]
        [InlineData("0.0.0.1+87776c971f1eacd99e6e178f3aea2caeb929293b", "0.0.0.1+87776c9")]
        [InlineData("0.0.0.1", "0.0.0.1")]
        [InlineData("0.0.0.1+", "0.0.0.1")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Format_ShortensRevisionHash(string? input, string expected)
        {
            Assert.Equal(expected, AppVersionFormatter.Format(input));
        }
    }
}
