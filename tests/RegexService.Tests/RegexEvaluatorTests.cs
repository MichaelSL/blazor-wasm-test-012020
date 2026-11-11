using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Models;
using BlazorWasmRegex.Shared.Services;
using Xunit;

namespace RegexService.Tests
{
    public class RegexEvaluatorTests
    {
        private readonly RegexEvaluator _evaluator = new();

        [Fact]
        public void Evaluate_KeepsEveryLineInOrder()
        {
            var result = _evaluator.Evaluate("abc123\r\n\nno digits\n456", new Regex(@"\d+"), null);

            Assert.Equal(new[] { "abc123", "", "no digits", "456" }, result.Lines.Select(l => l.Text));
            Assert.Equal(new[] { 1, 2, 3, 4 }, result.Lines.Select(l => l.Number));
            Assert.Equal(new[] { 1, 0, 0, 1 }, result.Lines.Select(l => l.Matches.Count));
            Assert.Equal(2, result.TotalMatches);
            Assert.Equal(2, result.MatchedLines);
            Assert.Equal(3, result.NonEmptyLines);
            Assert.True(result.ElapsedMilliseconds >= 0);
        }

        [Fact]
        public void Evaluate_DoesNotTrimOrDeduplicateLines()
        {
            var result = _evaluator.Evaluate(" a \n a ", new Regex("a"), null);

            Assert.Equal(2, result.Lines.Count);
            Assert.All(result.Lines, l => Assert.Equal(1, l.Matches.Single().Index));
        }

        [Fact]
        public void Evaluate_EmptyLinesAreNotEvaluated()
        {
            // ^$ would match an empty string, but empty lines are only placeholders.
            var result = _evaluator.Evaluate("\n", new Regex("^$"), "x");

            Assert.All(result.Lines, l =>
            {
                Assert.True(l.IsEmpty);
                Assert.Empty(l.Matches);
                Assert.Empty(l.Split);
                Assert.Null(l.Replace);
            });
        }

        [Fact]
        public void Evaluate_CapturesAllGroupsWithNumbersAndNames()
        {
            var result = _evaluator.Evaluate("john@example.com", new Regex(@"(?<user>\w+)@(\w+)\.(?<tld>\w+)"), null);
            var match = result.Lines.Single().Matches.Single();

            Assert.Equal(new[] { 0, 1, 2, 3 }, match.Groups.Select(g => g.Number));
            Assert.Equal(new[] { "0", "1", "user", "tld" }, match.Groups.Select(g => g.Name));
            Assert.Equal(new[] { "john@example.com", "example", "john", "com" }, match.Groups.Select(g => g.Value));
            Assert.Equal(new[] { false, false, true, true }, match.Groups.Select(g => g.IsNamed));
            Assert.Equal(3, result.CapturedGroups);
        }

        [Fact]
        public void Evaluate_SplitIncludesCapturedGroups()
        {
            var result = _evaluator.Evaluate("a1b2c", new Regex(@"(\d)"), null);

            Assert.Equal(new[] { "a", "1", "b", "2", "c" }, result.Lines.Single().Split);
            Assert.Equal(5, result.SplitParts);
        }

        [Fact]
        public void Evaluate_ReplaceMarksInsertedText()
        {
            var result = _evaluator.Evaluate("john@example.com, x", new Regex(@"(?<user>\w+)@(?<domain>[\w.]+)"), "${domain}/${user}");
            var replace = result.Lines.Single().Replace;

            Assert.NotNull(replace);
            Assert.Equal("example.com/john, x", replace.Output);
            Assert.Equal(new[] { ("example.com/john", true), (", x", false) },
                replace.Segments.Select(s => (s.Text, s.Inserted)));
            Assert.Equal(1, result.ReplacedLines);
        }

        [Fact]
        public void Evaluate_ReplaceWithRightToLeftKeepsSegmentsInTextOrder()
        {
            var result = _evaluator.Evaluate("a1b2", new Regex(@"\d", RegexOptions.RightToLeft), "#");
            var replace = result.Lines.Single().Replace;

            Assert.NotNull(replace);
            Assert.Equal("a#b#", replace.Output);
            Assert.Equal("a#b#", string.Concat(replace.Segments.Select(s => s.Text)));
        }

        [Fact]
        public void Evaluate_NoReplacementMeansNoReplaceResult()
        {
            var result = _evaluator.Evaluate("a", new Regex("a"), "");
            Assert.Null(result.Lines.Single().Replace);
        }

        [Fact]
        public void Evaluate_ThrowsTimeoutInsideTheCall()
        {
            var regex = new Regex(@"^(a+)+$", RegexOptions.None, TimeSpan.FromMilliseconds(50));
            Assert.Throws<RegexMatchTimeoutException>(() => _evaluator.Evaluate(new string('a', 40) + "!", regex, null));
        }

        [Fact]
        public void HighlightLine_SplitsTextAndMatchesWithInnermostGroupColors()
        {
            var regex = new Regex(@"(?<user>\w+)@(?<domain>\w+)");
            var result = _evaluator.Evaluate("hi a@b and c@d", regex, null);
            var line = result.Lines.Single();

            var spans = _evaluator.HighlightLine(line.Text, line.Matches);

            Assert.Equal(new int?[] { null, 0, null, 1 }, spans.Select(s => s.MatchIndex));
            Assert.Equal("hi a@b and c@d", string.Concat(spans.SelectMany(s => s.Runs).Select(r => r.Text)));
            // user = group 1 (slot 1), domain = group 2 (slot 2), '@' is outside any group.
            Assert.Equal(new[] { ("a", 1), ("@", 0), ("b", 2) }, spans[1].Runs.Select(r => (r.Text, r.Slot)));
        }

        [Fact]
        public void HighlightLine_NestedGroupWinsOverOuter()
        {
            var regex = new Regex(@"(a(b)c)");
            var line = _evaluator.Evaluate("abc", regex, null).Lines.Single();

            var runs = _evaluator.HighlightLine(line.Text, line.Matches).Single().Runs;

            Assert.Equal(new[] { ("a", 1), ("b", 2), ("c", 1) }, runs.Select(r => (r.Text, r.Slot)));
        }

        [Fact]
        public void HighlightLine_OrdersRightToLeftMatches()
        {
            var line = _evaluator.Evaluate("a1b2", new Regex(@"\d", RegexOptions.RightToLeft), null).Lines.Single();
            Assert.Equal(3, line.Matches[0].Index); // engine order: last match first

            var spans = _evaluator.HighlightLine(line.Text, line.Matches);

            Assert.Equal(new int?[] { null, 1, null, 0 }, spans.Select(s => s.MatchIndex));
        }

        [Fact]
        public void HighlightLine_EmptyMatchesHaveNoRuns()
        {
            var line = _evaluator.Evaluate("ab", new Regex("x*"), null).Lines.Single();

            var spans = _evaluator.HighlightLine(line.Text, line.Matches);

            Assert.Equal(3, spans.Count(s => s.IsEmptyMatch));
            Assert.Equal("ab", string.Concat(spans.SelectMany(s => s.Runs).Select(r => r.Text)));
        }

        [Fact]
        public void MatchId_CombinesLineAndMatchIndex()
        {
            var line = new LineResult(3, "x", [], [], null);
            Assert.Equal("3-0", line.MatchId(0));
        }
    }
}
