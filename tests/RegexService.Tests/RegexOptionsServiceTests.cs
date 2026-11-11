using System.Text.Json;
using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Models;
using BlazorWasmRegex.Shared.Services;
using Xunit;

namespace RegexService.Tests
{
    public class RegexOptionsServiceTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
        private readonly RegexOptionsService _service = new();

        private static RegexOptionsData Opts(
            bool ignoreCase = false, bool multiline = false, bool singleline = false,
            bool anyNewLine = false, bool nonBacktracking = false, bool explicitCapture = false,
            bool ignorePatternWhitespace = false, bool rightToLeft = false,
            bool cultureInvariant = false, bool ecmaScript = false) =>
            new(ignoreCase, multiline, singleline, anyNewLine, nonBacktracking, explicitCapture,
                ignorePatternWhitespace, rightToLeft, cultureInvariant, ecmaScript);

        // ---- ToRegexOptions ----

        [Fact]
        public void ToRegexOptions_Default_IsNone()
        {
            Assert.Equal(RegexOptions.None, _service.ToRegexOptions(RegexOptionsData.Default));
        }

        [Fact]
        public void ToRegexOptions_MapsEveryFlag_AndNeverAddsCompiled()
        {
            var all = new RegexOptionsData(true, true, true, true, true, true, true, true, true, true);

            var expected = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline
                | RegexOptions.AnyNewLine | RegexOptions.NonBacktracking | RegexOptions.ExplicitCapture
                | RegexOptions.IgnorePatternWhitespace | RegexOptions.RightToLeft
                | RegexOptions.CultureInvariant | RegexOptions.ECMAScript;
            Assert.Equal(expected, _service.ToRegexOptions(all));
        }

        // ---- Validate: must agree with what the Regex constructor accepts ----

        public static TheoryData<RegexOptionsData, bool> Combinations => new()
        {
            { Opts(), true },
            { Opts(ignoreCase: true, multiline: true, singleline: true, explicitCapture: true,
                   ignorePatternWhitespace: true, rightToLeft: true, cultureInvariant: true, anyNewLine: true), true },
            { Opts(ignoreCase: true, multiline: true, singleline: true, explicitCapture: true,
                   ignorePatternWhitespace: true, cultureInvariant: true, nonBacktracking: true), true },
            { Opts(ecmaScript: true, ignoreCase: true, multiline: true), true },
            { Opts(ecmaScript: true, cultureInvariant: true), true },
            { Opts(ecmaScript: true, singleline: true), false },
            { Opts(ecmaScript: true, explicitCapture: true), false },
            { Opts(ecmaScript: true, ignorePatternWhitespace: true), false },
            { Opts(ecmaScript: true, rightToLeft: true), false },
            { Opts(ecmaScript: true, nonBacktracking: true), false },
            { Opts(ecmaScript: true, anyNewLine: true), false },
            { Opts(ecmaScript: true, anyNewLine: true, ignoreCase: true, multiline: true), false },
            { Opts(nonBacktracking: true, rightToLeft: true), false },
            { Opts(nonBacktracking: true, anyNewLine: true), false },
            { Opts(nonBacktracking: true, anyNewLine: true, multiline: true), false },
            { Opts(anyNewLine: true, rightToLeft: true), true },
            { Opts(anyNewLine: true, singleline: true, multiline: true), true },
        };

        [Theory]
        [MemberData(nameof(Combinations))]
        public void Validate_MatchesRegexConstructor(RegexOptionsData data, bool valid)
        {
            Assert.Equal(valid, _service.Validate(data).Count == 0);

            // Ground truth: the .NET 11 regex constructor.
            var options = _service.ToRegexOptions(data);
            if (valid)
            {
                _ = new Regex("a", options, Timeout);
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new Regex("a", options, Timeout));
            }
        }

        // ---- TryCreateRegex ----

        [Fact]
        public void TryCreateRegex_Valid_SetsOptionsAndTimeout()
        {
            var result = _service.TryCreateRegex("^a$", Opts(ignoreCase: true, multiline: true, anyNewLine: true), Timeout);

            Assert.True(result.Success);
            Assert.Null(result.Error);
            Assert.Equal(Timeout, result.Regex!.MatchTimeout);
            Assert.Equal(RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.AnyNewLine, result.Regex.Options);
        }

        [Fact]
        public void TryCreateRegex_InvalidCombination_ReturnsFriendlyError()
        {
            var result = _service.TryCreateRegex("a", Opts(nonBacktracking: true, rightToLeft: true), Timeout);

            Assert.False(result.Success);
            Assert.Contains("NonBacktracking cannot be combined with RightToLeft", result.Error);
        }

        [Theory]
        [InlineData(@"(a)\1")]
        [InlineData(@"(?<n>a)\k<n>")]
        [InlineData(@"(?=a)a")]
        [InlineData(@"(?<!a)b")]
        [InlineData(@"(?>a+)")]
        [InlineData(@"(?(a)a|b)")]
        [InlineData(@"\Ga")]
        public void TryCreateRegex_NonBacktrackingUnsupportedConstruct_ReturnsErrorInsteadOfThrowing(string pattern)
        {
            var result = _service.TryCreateRegex(pattern, Opts(nonBacktracking: true), Timeout);

            Assert.False(result.Success);
            Assert.StartsWith("NonBacktracking does not support this pattern: it contains ", result.Error);
            Assert.Null(result.ParseError);
        }

        [Theory]
        [InlineData("RegexOptions.NonBacktracking is not supported in conjunction with expressions containing: 'backreference (\\ number)'.", "backreference (\\ number)")]
        // Blazor WASM trims resource strings, so the message is only resource keys.
        [InlineData("NotSupported_NonBacktrackingConflictingExpression, ExpressionDescription_Backreference", "backreference")]
        [InlineData("NotSupported_NonBacktrackingConflictingExpression, ExpressionDescription_PositiveLookaroundAssertion", "positive lookaround assertion")]
        [InlineData("something else", null)]
        [InlineData(null, null)]
        public void DescribeUnsupportedConstruct_HandlesFullAndTrimmedMessages(string? message, string? expected)
        {
            Assert.Equal(expected, RegexOptionsService.DescribeUnsupportedConstruct(message));
        }

        [Fact]
        public void TryCreateRegex_NonBacktrackingSupportedPattern_Succeeds()
        {
            var result = _service.TryCreateRegex(@"^(\w+)@(\w+)\.com$", Opts(nonBacktracking: true, ignoreCase: true), Timeout);

            Assert.True(result.Success);
            Assert.Single(result.Regex!.Matches("Joe@Example.com"));
        }

        [Fact]
        public void TryCreateRegex_ParseError_ReturnsParseDetails()
        {
            var result = _service.TryCreateRegex("(abc", RegexOptionsData.Default, Timeout);

            Assert.False(result.Success);
            Assert.NotNull(result.ParseError);
            Assert.Equal(RegexParseError.InsufficientClosingParentheses, result.ParseError!.Error);
        }

        [Fact]
        public void AnyNewLine_WithMultiline_TreatsCrLfAndUnicodeSeparatorsAsLineBreaks()
        {
            const string input = "a\u2028b\r\nc";

            var plain = _service.TryCreateRegex("^.+$", Opts(multiline: true), Timeout).Regex!;
            var anyNewLine = _service.TryCreateRegex("^.+$", Opts(multiline: true, anyNewLine: true), Timeout).Regex!;

            Assert.Equal(new[] { "a\u2028b\r", "c" }, plain.Matches(input).Select(m => m.Value));
            Assert.Equal(new[] { "a", "b", "c" }, anyNewLine.Matches(input).Select(m => m.Value));
        }

        // ---- Storage format ----

        [Fact]
        public void Deserialize_OldThreeFieldJson_DefaultsNewFlagsToFalse()
        {
            // Exactly what the previous RegexTester.RegexOptionsData(bool, bool, bool) wrote.
            const string stored = "{\"IgnoreCase\":true,\"Multiline\":false,\"Singleline\":true}";

            var data = _service.Deserialize(stored);

            Assert.Equal(new RegexOptionsData(true, false, true), data);
            Assert.False(data!.AnyNewLine);
            Assert.False(data.NonBacktracking);
            Assert.False(data.ECMAScript);
        }

        [Fact]
        public void SerializeDeserialize_NewJson_RoundTrips()
        {
            var original = Opts(ignoreCase: true, anyNewLine: true, explicitCapture: true,
                ignorePatternWhitespace: true, rightToLeft: true, cultureInvariant: true);

            var json = _service.Serialize(original);

            Assert.Equal(original, _service.Deserialize(json));
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.GetProperty("AnyNewLine").GetBoolean());
            Assert.True(doc.RootElement.GetProperty("IgnoreCase").GetBoolean());
        }

        [Theory]
        [InlineData("abc|True|False|True", true, false, true)]
        [InlineData("a|b|c|False|True|False", false, true, false)] // pattern containing '|'
        [InlineData("x|true|true|true", true, true, true)]
        public void Deserialize_LegacyPipeFormat_ReadsLastThreeValues(string stored, bool ignoreCase, bool multiline, bool singleline)
        {
            Assert.Equal(new RegexOptionsData(ignoreCase, multiline, singleline), _service.Deserialize(stored));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not json")]
        [InlineData("a|b")]
        public void Deserialize_EmptyOrUnrecognised_ReturnsNull(string? stored)
        {
            Assert.Null(_service.Deserialize(stored));
        }
    }
}
