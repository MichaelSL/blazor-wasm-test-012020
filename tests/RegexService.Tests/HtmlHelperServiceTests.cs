using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Services;
using Xunit;

namespace RegexService.Tests
{
    public class HtmlHelperServiceTests
    {
        private readonly HtmlHelperService _service = new();

        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.RightToLeft)]
        public void GetMarkedSpans_MarksEachMatchOnceInInputOrder(RegexOptions options)
        {
            const string input = "a1b2c";
            var matches = new Regex(@"\d", options).Matches(input);

            Assert.Equal("a<span class='m'>1</span>b<span class='m'>2</span>c",
                _service.GetMarkedSpans(input, matches, "m"));
        }

        [Fact]
        public void GetMarkedSpans_RightToLeftEmptyMatches_DoesNotDuplicateText()
        {
            const string input = "ab";
            var matches = new Regex("x*", RegexOptions.RightToLeft).Matches(input);

            Assert.Equal("<span class='m'></span>a<span class='m'></span>b<span class='m'></span>",
                _service.GetMarkedSpans(input, matches, "m"));
        }

        [Fact]
        public void GetMarkedSpans_EncodesHtml()
        {
            const string input = "<b>&</b>";
            var matches = new Regex("&").Matches(input);

            Assert.Equal("&lt;b&gt;<span class='m'>&amp;</span>&lt;/b&gt;",
                _service.GetMarkedSpans(input, matches, "m"));
        }

        [Fact]
        public void GetMarkedSpans_NoMatches_ReturnsEncodedInput()
        {
            const string input = "a<b";
            var matches = new Regex(@"\d").Matches(input);

            Assert.Equal("a&lt;b", _service.GetMarkedSpans(input, matches, "m"));
        }
    }
}
