using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Services;
using Xunit;

namespace RegexService.Tests
{
    public class PatternHighlighterTests
    {
        private static string Render(string pattern, bool explicitCapture = false, bool ignoreWhitespace = false,
            Regex? regex = null, RegexParseException? error = null) =>
            string.Concat(PatternHighlighter.Highlight(pattern, explicitCapture, ignoreWhitespace, regex, error)
                .Select(s => s.CssClass is null ? s.Text : $"<{s.CssClass}>{s.Text}</>"));

        [Fact]
        public void Highlight_PreservesPatternText()
        {
            const string pattern = @"(?<user>[\w.+-]+)@(?<domain>[\w-]+\.[\w.]+)|\d{2,3}?$";
            var text = string.Concat(PatternHighlighter.Highlight(pattern).Select(s => s.Text));
            Assert.Equal(pattern, text);
        }

        [Fact]
        public void Highlight_ColorsTokens()
        {
            Assert.Equal(@"<tok-esc>\d</><tok-quant>+?</>a<tok-class>[b-c]</><tok-meta>|</><tok-meta>^</>",
                Render(@"\d+?a[b-c]|^"));
        }

        [Fact]
        public void Highlight_NumbersUnnamedGroupsBeforeNamedOnes()
        {
            // .NET: (?<n>..) is group 2 because the unnamed group comes first in numbering.
            Assert.Equal("<gc2>(?<n></>a<gc2>)</><gc1>(</>b<gc1>)</>", Render("(?<n>a)(b)"));
        }

        [Fact]
        public void Highlight_UsesRegexGroupNumbersWhenGiven()
        {
            var regex = new Regex("(?<n>a)(b)");
            Assert.Equal(2, regex.GroupNumberFromName("n"));
            Assert.Equal("<gc2>(?<n></>a<gc2>)</><gc1>(</>b<gc1>)</>", Render("(?<n>a)(b)", regex: regex));
        }

        [Fact]
        public void Highlight_NonCapturingGroupsAndLookaroundsAreMeta()
        {
            Assert.Equal("<tok-meta>(?:</>a<tok-meta>)</><tok-meta>(?<=</>b<tok-meta>)</><tok-meta>(?></>c<tok-meta>)</>",
                Render("(?:a)(?<=b)(?>c)"));
        }

        [Fact]
        public void Highlight_ExplicitCaptureMakesPlainGroupsNonCapturing()
        {
            Assert.Equal("<tok-meta>(</>a<tok-meta>)</>", Render("(a)", explicitCapture: true));
            Assert.Equal("<tok-meta>(?n)</><tok-meta>(</>a<tok-meta>)</>", Render("(?n)(a)"));
        }

        [Fact]
        public void Highlight_CharacterClassSubtractionIsOneToken()
        {
            Assert.Equal("<tok-class>[a-z-[aeiou]]</>x", Render("[a-z-[aeiou]]x"));
        }

        [Fact]
        public void Highlight_IgnorePatternWhitespaceComments()
        {
            Assert.Equal("a <tok-comment># note</>", Render("a # note", ignoreWhitespace: true));
            Assert.Equal("a # note", Render("a # note"));
        }

        [Fact]
        public void Highlight_ConditionalTestIsNotACaptureGroup()
        {
            var html = Render("(?<x>a)?(?(x)b|c)");
            Assert.Contains("<tok-meta>(?(x)</>", html);
        }

        [Fact]
        public void Highlight_GroupColorsWrapAfterSix()
        {
            Assert.Equal(1, PatternHighlighter.SlotFor(1));
            Assert.Equal(6, PatternHighlighter.SlotFor(6));
            Assert.Equal(1, PatternHighlighter.SlotFor(7));
            Assert.Equal(0, PatternHighlighter.SlotFor(0));
        }

        [Theory]
        [InlineData("a)", "a<tok-meta tok-err>)</>")]
        [InlineData("*a", "<tok-quant tok-err>*</>a")]
        [InlineData("(abc", "<gc1>(</>abc<tok-err tok-err-end> </>")]
        [InlineData("[abc", "<tok-class>[abc</><tok-err tok-err-end> </>")]
        public void Highlight_MarksParseErrorPosition(string pattern, string expected)
        {
            var error = Assert.Throws<RegexParseException>(() => new Regex(pattern));
            Assert.Equal(expected, Render(pattern, error: error));
        }

        [Fact]
        public void Highlight_ErrorInsideTokenUnderlinesOneCharacter()
        {
            // "Nested quantifier" is reported just past the second '*'.
            var error = Assert.Throws<RegexParseException>(() => new Regex("a**"));
            Assert.Equal("a<tok-quant>*</><tok-quant tok-err>*</>", Render("a**", error: error));
        }

        [Fact]
        public void Highlight_EmptyPattern()
        {
            Assert.Empty(PatternHighlighter.Highlight(""));
            Assert.Empty(PatternHighlighter.Highlight(null));
        }

        [Fact]
        public void DescribeParseError_SplitsWords()
        {
            Assert.Equal("Insufficient closing parentheses",
                PatternHighlighter.DescribeParseError(RegexParseError.InsufficientClosingParentheses));
        }
    }
}
