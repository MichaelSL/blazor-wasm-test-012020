using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Services;
using Xunit;

namespace RegexService.Tests
{
    public class RegexServiceTests
    {
        private readonly BlazorWasmRegex.Shared.Services.RegexService _service = new();

        [Fact]
        public void GetMatches_ReturnsExpectedMatchesAndElapsedTime()
        {
            var tests = new[] { "abc123", "no digits", "456" };
            var regex = new Regex("\\d+", RegexOptions.Compiled);

            var (elapsed, matches) = _service.GetMatches(tests, regex);

            Assert.True(elapsed >= 0);
            Assert.Equal(3, matches.Count);
            Assert.Equal("abc123", matches.Keys.First());
            Assert.Single(matches["abc123"]);
            Assert.Empty(matches["no digits"]);
            Assert.Single(matches["456"]);
        }

        [Fact]
        public void GetMatchedStrings_FiltersNonMatchingInputs()
        {
            var tests = new[] { "foo", "bar", "foobar" };
            var regex = new Regex("foo", RegexOptions.Compiled);

            var result = _service.GetMatchedStrings(tests, regex).ToArray();

            Assert.Equal(new[] { "foo", "foobar" }, result);
        }

        [Fact]
        public void GetMatchedStrings_AppliesHighlighterViaReplace()
        {
            var tests = new[] { "abc123def", "no digits", "456" };
            var regex = new Regex("\\d+", RegexOptions.Compiled);

            string Highlighter(Match m) => $"[{m.Value}]";

            var result = _service.GetMatchedStrings(tests, regex, Highlighter).ToArray();

            Assert.Equal(2, result.Length);
            Assert.Contains("abc[123]def", result);
            Assert.Contains("[456]", result);
        }

        [Fact]
        public void GetSplitList_SplitsAndFiltersEmptyGroups()
        {
            var tests = new[] { "a,b,c", ",,,", "x,y,,z" };
            var regex = new Regex(",", RegexOptions.Compiled);

            var result = _service.GetSplitList(tests, regex).ToArray();

            // Entry with only delimiters ",,," should produce only empty segments and be filtered out
            Assert.Equal(2, result.Length);

            Assert.Equal(new[] { "a", "b", "c" }, result[0]);
            Assert.Equal(new[] { "x", "y", "", "z" }, result[1]);
        }

        [Fact]
        public void GetSplitList_FiltersWhenAllSegmentsAreEmpty()
        {
            var tests = new[] { ",,," };
            var regex = new Regex(",", RegexOptions.Compiled);

            var result = _service.GetSplitList(tests, regex).ToArray();
            Assert.Empty(result);
        }
    }
}
