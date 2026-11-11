using System.Text.Json;
using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Services;
using Xunit;

namespace RegexService.Tests
{
    public class MatchExporterTests
    {
        private static BlazorWasmRegex.Shared.Models.EvaluationResult Evaluate(string tests, string pattern) =>
            new RegexEvaluator().Evaluate(tests, new Regex(pattern), null);

        [Fact]
        public void ToJson_KeysByTestStringAndSkipsGroupZero()
        {
            var json = MatchExporter.ToJson(Evaluate("a1\nnone\na1", @"[a-z](?<d>\d)"));

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Single(root.EnumerateObject()); // "none" has no matches, duplicate "a1" exported once
            var match = root.GetProperty("a1")[0];
            Assert.Equal("a1", match.GetProperty("Value").GetString());
            Assert.Equal(0, match.GetProperty("Index").GetInt32());
            Assert.Equal(2, match.GetProperty("Length").GetInt32());
            var group = Assert.Single(match.GetProperty("Groups").EnumerateArray());
            Assert.Equal("1", group.GetProperty("Value").GetString());
        }

        [Fact]
        public void ToCsv_QuotesAndEscapes()
        {
            var csv = MatchExporter.ToCsv(Evaluate("say \"hi\"", "\"hi\""));

            Assert.Equal("Test String,Match Value,Match Index,Match Length\n\"say \"\"hi\"\"\",\"\"\"hi\"\"\",4,4\n",
                csv.ReplaceLineEndings("\n"));
        }

        [Fact]
        public void ToText_SummarisesMatches()
        {
            var text = MatchExporter.ToText(@"\d", Evaluate("a1b2\nx", @"\d")).ReplaceLineEndings("\n");

            Assert.StartsWith("Regex Pattern: \\d\nTotal Matches: 2\nTest Strings with Matches: 1\n", text);
            Assert.Contains("  - Value: \"2\" (Index: 3, Length: 1)", text);
        }
    }
}
