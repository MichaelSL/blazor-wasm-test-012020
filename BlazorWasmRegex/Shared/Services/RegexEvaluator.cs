#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Interfaces;
using BlazorWasmRegex.Shared.Models;

namespace BlazorWasmRegex.Shared.Services
{
    public class RegexEvaluator : IRegexEvaluator
    {
        public EvaluationResult Evaluate(string? tests, Regex regex, string? replacement)
        {
            ArgumentNullException.ThrowIfNull(regex);

            var lines = SplitLines(tests);
            var groupNumbers = regex.GetGroupNumbers();
            var results = new List<LineResult>(lines.Count);
            var stopwatch = Stopwatch.StartNew();

            for (var i = 0; i < lines.Count; i++)
            {
                var text = lines[i];
                if (text.Length == 0)
                {
                    results.Add(new LineResult(i + 1, text, [], [], null));
                    continue;
                }

                // Materialize everything here: MatchCollection and Split are lazy, and a timeout must
                // surface from this call rather than while the UI renders.
                var matches = regex.Matches(text)
                    .Select(m => ToMatchResult(m, regex, groupNumbers))
                    .ToList();
                var split = regex.Split(text);
                var replace = string.IsNullOrEmpty(replacement) ? null : Replace(text, regex, replacement);

                results.Add(new LineResult(i + 1, text, matches, split, replace));
            }

            stopwatch.Stop();
            return new EvaluationResult(results, stopwatch.ElapsedMilliseconds);
        }

        public IReadOnlyList<HighlightSpan> HighlightLine(string text, IReadOnlyList<MatchResult> matches)
        {
            ArgumentNullException.ThrowIfNull(text);
            ArgumentNullException.ThrowIfNull(matches);

            var spans = new List<HighlightSpan>();
            var pos = 0;
            // Walk in ascending order: with RightToLeft the engine returns matches last-first.
            var ordered = matches
                .Select((m, k) => (Match: m, Index: k))
                .OrderBy(x => x.Match.Index)
                .ThenBy(x => x.Match.Length);

            foreach (var (m, k) in ordered)
            {
                if (m.Index < pos)
                {
                    continue; // defensive: .NET matches never overlap
                }
                if (m.Index > pos)
                {
                    spans.Add(new HighlightSpan(null, [new HighlightRun(text[pos..m.Index], 0)]));
                }
                spans.Add(new HighlightSpan(k, HighlightMatch(m)));
                pos = m.Index + m.Length;
            }
            if (pos < text.Length)
            {
                spans.Add(new HighlightSpan(null, [new HighlightRun(text[pos..], 0)]));
            }
            return spans;
        }

        public IReadOnlyList<HighlightRun> HighlightMatch(MatchResult match)
        {
            ArgumentNullException.ThrowIfNull(match);
            if (match.Length == 0)
            {
                return [];
            }

            // Each character takes the color of the innermost (shortest) successful group covering it.
            var owner = new GroupResult?[match.Length];
            foreach (var g in match.Groups)
            {
                if (g.Number == 0 || !g.Success || g.Length == 0)
                {
                    continue;
                }
                var from = Math.Max(g.Index - match.Index, 0);
                var to = Math.Min(g.Index - match.Index + g.Length, match.Length);
                for (var c = from; c < to; c++)
                {
                    if (owner[c] is not { } current || g.Length <= current.Length)
                    {
                        owner[c] = g;
                    }
                }
            }

            var runs = new List<HighlightRun>();
            var start = 0;
            for (var c = 1; c <= match.Length; c++)
            {
                if (c == match.Length || owner[c] != owner[start])
                {
                    runs.Add(new HighlightRun(match.Value[start..c], owner[start]?.Slot ?? 0));
                    start = c;
                }
            }
            return runs;
        }

        internal static List<string> SplitLines(string? tests)
        {
            if (string.IsNullOrEmpty(tests))
            {
                return [];
            }
            return tests.Split('\n').Select(l => l.EndsWith('\r') ? l[..^1] : l).ToList();
        }

        private static MatchResult ToMatchResult(Match m, Regex regex, int[] groupNumbers)
        {
            var groups = new List<GroupResult>(groupNumbers.Length);
            foreach (var number in groupNumbers)
            {
                var g = m.Groups[number];
                groups.Add(new GroupResult(number, regex.GroupNameFromNumber(number), g.Success, g.Index, g.Length, g.Value));
            }
            return new MatchResult(m.Index, m.Length, m.Value, groups);
        }

        private static ReplaceResult Replace(string text, Regex regex, string replacement)
        {
            var inserted = new List<(int Index, int Length, string Text)>();
            var output = regex.Replace(text, m =>
            {
                var result = m.Result(replacement);
                inserted.Add((m.Index, m.Length, result));
                return result;
            });

            var segments = new List<ReplaceSegment>();
            var pos = 0;
            foreach (var (index, length, value) in inserted.OrderBy(x => x.Index))
            {
                if (index > pos)
                {
                    segments.Add(new ReplaceSegment(text[pos..index], false));
                }
                if (value.Length > 0)
                {
                    segments.Add(new ReplaceSegment(value, true));
                }
                pos = index + length;
            }
            if (pos < text.Length)
            {
                segments.Add(new ReplaceSegment(text[pos..], false));
            }
            return new ReplaceResult(output, segments);
        }
    }
}
