#nullable enable
using System.Collections.Generic;
using System.Linq;
using BlazorWasmRegex.Shared.Services;

namespace BlazorWasmRegex.Shared.Models
{
    /// <summary>Result of running a regex over every line of the test input.</summary>
    /// <param name="Lines">One entry per input line (including empty lines, which are not evaluated).</param>
    /// <param name="ElapsedMilliseconds">Time spent matching, splitting and replacing.</param>
    public sealed record EvaluationResult(IReadOnlyList<LineResult> Lines, long ElapsedMilliseconds)
    {
        public int TotalMatches => Lines.Sum(l => l.Matches.Count);
        public int MatchedLines => Lines.Count(l => l.Matches.Count > 0);
        public int NonEmptyLines => Lines.Count(l => !l.IsEmpty);
        public int CapturedGroups
        {
            get
            {
                var count = 0;
                foreach (var line in Lines)
                    foreach (var match in line.Matches)
                        foreach (var group in match.Groups)
                            if (group.Number > 0 && group.Success)
                                count++;
                return count;
            }
        }
        public int SplitParts => Lines.Where(l => !l.IsEmpty).Sum(l => l.Split.Count);
        public int ReplacedLines => Lines.Count(l => l.Replace is not null && l.Matches.Count > 0);

        public static EvaluationResult Empty { get; } = new([], 0);
    }

    /// <param name="Number">1-based line number in the test input.</param>
    /// <param name="Text">The line without its line break.</param>
    /// <param name="Matches">Matches in the order the engine returned them (descending with RightToLeft).</param>
    /// <param name="Split"><c>Regex.Split</c> of the line (includes captured groups, as .NET does).</param>
    /// <param name="Replace"><c>Regex.Replace</c> of the line, or <c>null</c> without a replacement pattern.</param>
    public sealed record LineResult(
        int Number,
        string Text,
        IReadOnlyList<MatchResult> Matches,
        IReadOnlyList<string> Split,
        ReplaceResult? Replace)
    {
        public bool IsEmpty => Text.Length == 0;

        /// <summary>Stable id of a match for linking the same match across views (e.g. "3-0").</summary>
        public string MatchId(int matchIndex) => $"{Number}-{matchIndex}";
    }

    /// <param name="Groups">Every group of the regex, starting with group 0 (the whole match).</param>
    public sealed record MatchResult(int Index, int Length, string Value, IReadOnlyList<GroupResult> Groups);

    public sealed record GroupResult(int Number, string Name, bool Success, int Index, int Length, string Value)
    {
        /// <summary>Color slot shared with the pattern field (0 for the whole match).</summary>
        public int Slot => PatternHighlighter.SlotFor(Number);

        /// <summary>True for groups with an explicit name (not just a number).</summary>
        public bool IsNamed => Name != Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <param name="Output">The replaced line.</param>
    /// <param name="Segments">The output split into unchanged text and inserted replacements.</param>
    public sealed record ReplaceResult(string Output, IReadOnlyList<ReplaceSegment> Segments);

    public sealed record ReplaceSegment(string Text, bool Inserted);

    /// <summary>
    /// A piece of a highlighted line: plain text (<see cref="MatchIndex"/> is <c>null</c>) or one match
    /// split into runs colored by the innermost capture group.
    /// </summary>
    public sealed record HighlightSpan(int? MatchIndex, IReadOnlyList<HighlightRun> Runs)
    {
        public bool IsMatch => MatchIndex is not null;
        public bool IsEmptyMatch => IsMatch && Runs.Count == 0;
    }

    /// <param name="Slot">Capture-group color slot (1..6), or 0 for text not inside a group.</param>
    public sealed record HighlightRun(string Text, int Slot);
}
